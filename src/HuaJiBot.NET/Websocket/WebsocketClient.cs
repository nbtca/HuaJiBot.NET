using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using WatsonWebsocket;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
    private bool _isReconnecting;
    private int _reconnectAttempts;
    private bool _shouldReconnect = true; // 控制是否应该重连
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
                    _logger?.LogWarning(ex, $"Failed to set header {header.Key}");
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
            _logger?.LogInformation($"WebSocket connecting to {_url}");
            
            await Task.Run(() => _client.Start(), _cancellationTokenSource.Token);
            
            // 重置重连计数
            _reconnectAttempts = 0;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error starting WebSocket connection");
            OnClosed?.Invoke(new DisconnectionInfo
            {
                Type = DisconnectionType.Error,
                Reason = e.Message,
                Exception = e
            });
            
            // 启动自动重连
            _ = TryReconnectAsync();
        }
    }

    private void OnServerConnected(object? sender, EventArgs e)
    {
        _logger?.LogInformation("WebSocket connected");
        
        var connectionInfo = new ConnectionInfo
        {
            IsReconnect = _hasConnectedBefore,
            Timestamp = DateTimeOffset.Now
        };

        _hasConnectedBefore = true;
        OnConnected?.Invoke(connectionInfo);
    }

    private void OnServerDisconnected(object? sender, EventArgs e)
    {
        _logger?.LogInformation("WebSocket disconnected");
        
        OnClosed?.Invoke(new DisconnectionInfo
        {
            Type = DisconnectionType.ByServer,
            Reason = "Server disconnected",
            Timestamp = DateTimeOffset.Now
        });
        
        // 只要 _shouldReconnect 为 true，就启动自动重连
        if (_shouldReconnect && !_disposed)
        {
            _ = TryReconnectAsync();
        }
    }

    private async Task TryReconnectAsync()
    {
        if (_disposed || !_shouldReconnect)
            return;

        // 使用信号量防止并发重连
        if (!await _reconnectLock.WaitAsync(0))
            return;

        try
        {
            if (_isReconnecting)
                return;

            _isReconnecting = true;

            while (!_disposed && _shouldReconnect)
            {
                _reconnectAttempts++;
                
                // 计算延迟时间（指数退避，最大30秒）
                var delay = Math.Min(InitialReconnectDelay * (int)Math.Pow(2, _reconnectAttempts - 1), MaxReconnectDelay);
                
                _logger?.LogInformation($"Attempting to reconnect (attempt {_reconnectAttempts}) in {delay}ms...");
                
                try
                {
                    await Task.Delay(delay, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // 如果被取消但仍需要重连，继续尝试
                    if (_shouldReconnect && !_disposed)
                    {
                        _logger?.LogInformation("Reconnect delay cancelled, but will continue trying...");
                        await Task.Delay(delay); // 使用不带取消令牌的版本
                    }
                    else
                    {
                        break;
                    }
                }

                try
                {
                    if (_client.Connected)
                    {
                        _logger?.LogInformation("Already connected, stopping reconnect attempts");
                        break;
                    }

                    _logger?.LogInformation($"Reconnecting to {_url}...");
                    
                    // 尝试停止现有连接（如果有）
                    if (_client.Connected)
                    {
                        try
                        {
                            _client.Stop();
                        }
                        catch (Exception stopEx)
                        {
                            _logger?.LogDebug(stopEx, "Error stopping client before reconnect");
                        }
                    }
                    
                    await Task.Run(() => _client.Start());
                    
                    _logger?.LogInformation("Reconnected successfully");
                    _reconnectAttempts = 0;
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, $"Reconnect attempt {_reconnectAttempts} failed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during reconnection process");
        }
        finally
        {
            _isReconnecting = false;
            _reconnectLock.Release();
        }
    }

    /// <summary>
    /// 定期检查连接健康状态，如果断线则触发重连
    /// </summary>
    private async Task RunHealthCheckAsync()
    {
        _logger?.LogDebug("Health check task started");
        
        while (!_disposed && _shouldReconnect)
        {
            try
            {
                await Task.Delay(HealthCheckInterval, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // 如果被取消但仍需要运行健康检查，继续
                if (_shouldReconnect && !_disposed)
                {
                    await Task.Delay(HealthCheckInterval);
                }
                else
                {
                    break;
                }
            }

            try
            {
                // 检查连接状态
                if (!_client.Connected && !_isReconnecting)
                {
                    _logger?.LogWarning("Health check detected disconnection, triggering reconnect...");
                    _ = TryReconnectAsync();
                }
                else if (_client.Connected)
                {
                    _logger?.LogDebug("Health check: Connection is healthy");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during health check");
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
                _logger?.LogWarning("Cannot send message: WebSocket is not connected");
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
        _shouldReconnect = false; // 停止重连

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
                    _logger?.LogDebug(ex, "Error waiting for health check task to complete");
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
