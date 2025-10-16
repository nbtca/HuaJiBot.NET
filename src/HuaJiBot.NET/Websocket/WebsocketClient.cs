using System.Net.WebSockets;
using System.Text;
using HuaJiBot.NET.Logger;
using Newtonsoft.Json.Linq;
using WatsonWebsocket;

namespace HuaJiBot.NET.Websocket;

public class WebsocketClient : IWebsocketClient
{
    private readonly WatsonWsClient _client;
    private readonly ILogger? _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _hasConnectedBefore;
    private readonly Uri _url;
    private readonly Dictionary<string, string> _headers;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private int _connectAttempts;
    private Task? _healthCheckTask; // 健康检查任务
    private const int MaxReconnectDelay = 30000; // 最大重连延迟 30 秒
    private const int InitialReconnectDelay = 1000; // 初始重连延迟 1 秒
    private const int HealthCheckInterval = 5000; // 健康检查间隔 5 秒

    public WebsocketClient(
        string url,
        string? token = null,
        ILogger? logger = null,
        Dictionary<string, string>? headers = null
    )
        : this(new Uri(url), token, logger, headers) { }

    public WebsocketClient(
        Uri url,
        string? token = null,
        ILogger? logger = null,
        Dictionary<string, string>? headers = null
    )
    {
        _logger = logger;
        _url = url;
        _headers = headers ?? new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(token))
        {
            _headers["Authorization"] = $"Bearer {token}";
        }

        // Create Watson WebSocket client with Uri
        _client = new WatsonWsClient(url);

        // Configure client options with custom headers
        _client.ConfigureOptions(options =>
        {
            // Set custom headers on the underlying ClientWebSocket
            foreach (var header in _headers)
            {
                try
                {
                    options.SetRequestHeader(header.Key, header.Value);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Failed to set header {header.Key}");
                }
            }
        });

        // Subscribe to events
        _client.ServerConnected += OnServerConnected;
        _client.ServerDisconnected += OnServerDisconnected;
        _client.MessageReceived += OnMessageReceived;

        // Start connection
        _ = StartConnectionAsync();

        // Start health check
        _healthCheckTask = RunHealthCheckAsync();
    }

    private async Task StartConnectionAsync()
    {
        try
        {
            _logger?.Log($"WebSocket connecting to {_url}");

            await Task.Run(() => _client.Start(), _cancellationTokenSource.Token);

            // 重置重连计数
            _connectAttempts = 0;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error starting WebSocket connection");
            OnClosed?.Invoke(
                new DisconnectionInfo
                {
                    Type = DisconnectionType.Error,
                    Reason = e.Message,
                    Exception = e,
                }
            );
        }
    }

    private void OnServerConnected(object? sender, EventArgs e)
    {
        _logger?.Log("WebSocket connected");

        // 重置重连计数
        _connectAttempts = 0;

        var connectionInfo = new ConnectionInfo
        {
            IsReconnect = _hasConnectedBefore,
            Timestamp = DateTimeOffset.Now,
        };

        _hasConnectedBefore = true;
        OnConnected?.Invoke(connectionInfo);
    }

    private void OnServerDisconnected(object? sender, EventArgs e)
    {
        _logger?.Log("WebSocket disconnected");

        OnClosed?.Invoke(
            new DisconnectionInfo
            {
                Type = DisconnectionType.ByServer,
                Reason = "Server disconnected",
                Timestamp = DateTimeOffset.Now,
            }
        );
    }

    public async ValueTask ConnectAsync()
    {
        _logger?.LogDebug(nameof(ConnectAsync) + _url);
        // 使用信号量防止并发重连
        if (await _reconnectLock.WaitAsync(0))
        {
            try
            {
                _connectAttempts++;
                _logger?.Log("Reconnected successfully");
                if (await _client.StartWithTimeoutAsync(10))
                {
                    _connectAttempts = 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Reconnect attempt {_connectAttempts} failed");
            }
            finally
            {
                _reconnectLock.Release();
            }
        }
    }

    /// <summary>
    /// 定期检查连接健康状态，如果断线则触发重连
    /// </summary>
    private async Task RunHealthCheckAsync()
    {
        while (!_disposed)
        {
            try
            {
                // 检查连接状态
                if (!_client.Connected)
                {
                    // 计算延迟时间（简化的指数退避，1秒到30秒）
                    var delay = Math.Min(
                        InitialReconnectDelay << Math.Min(_connectAttempts - 1, 5),
                        MaxReconnectDelay
                    );

                    _logger?.Log(
                        $"Attempting to reconnect (attempt {_connectAttempts}) in {delay}ms..."
                    );
                    try
                    {
                        await Task.Delay(delay, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        if (_disposed)
                            break;
                    }
                    if (_disposed)
                        break;
                    await ConnectAsync();
                }
                else
                {
                    // 连接正常，重置重连计数
                    if (_connectAttempts > 0)
                    {
                        _connectAttempts = 0;
                    }

                    // 等待下一次健康检查
                    try
                    {
                        await Task.Delay(HealthCheckInterval, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        if (_disposed)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during health check");

                // 发生错误后等待一段时间再继续
                try
                {
                    await Task.Delay(HealthCheckInterval, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    if (_disposed)
                        break;
                }
            }
        }

        _logger?.LogDebug("Health check task stopped");
    }

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        // Fire and forget - don't block the event handler
        _ = ProcessMessageInternalAsync(e);
    }

    private async Task ProcessMessageInternalAsync(MessageReceivedEventArgs e)
    {
        try
        {
            var msg = Encoding.UTF8.GetString(e.Data);
            _logger?.LogDebug($"Received message: {msg}");
            await ProcessMessageAsync(msg);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing received message");
        }
    }

    private async ValueTask ProcessMessageAsync(string msg)
    {
        var jsonObject = JToken.Parse(msg);
        await (OnMessage?.Invoke(jsonObject) ?? ValueTask.CompletedTask);
    }

    public event Func<JToken, ValueTask>? OnMessage;
    public event Action<ConnectionInfo>? OnConnected;
    public event Action<DisconnectionInfo>? OnClosed;

    public void Send(string msg)
    {
        // Fire and forget - don't block
        _ = SendAsync(msg);
    }

    private async Task SendAsync(string msg)
    {
        try
        {
            if (!_client.Connected)
            {
                _logger?.Warn("Cannot send message: WebSocket is not connected");
                return;
            }

            await _client.SendAsync(msg, WebSocketMessageType.Text);
            _logger?.LogDebug($"Sent message: {msg}");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error sending message");
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _cancellationTokenSource.Cancel();

            if (_client.Connected)
            {
                _client.Stop();
            }

            // 等待健康检查任务完成
            if (_healthCheckTask != null)
            {
                try
                {
                    _healthCheckTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error waiting for health check task to complete");
                }
            }

            _client.Dispose();
            _cancellationTokenSource.Dispose();
            _reconnectLock.Dispose();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error disposing WebsocketClient");
        }
    }
}
