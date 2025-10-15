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
    }

    private async Task StartConnectionAsync()
    {
        try
        {
            _logger?.LogInformation($"WebSocket connecting to {_url}");
            
            await Task.Run(() => _client.Start(), _cancellationTokenSource.Token);
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
            var msg = Encoding.UTF8.GetString(e.Data.ToArray());
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

        try
        {
            _cancellationTokenSource.Cancel();
            
            if (_client.Connected)
            {
                _client.Stop();
            }

            _client.Dispose();
            _cancellationTokenSource.Dispose();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error disposing WebsocketClient");
        }
    }
}
