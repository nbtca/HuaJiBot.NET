using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.Authentication;
using System.Text;
using IWebsocketClientLite;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using WebsocketClientLite;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace HuaJiBot.NET.MQ;

// ReSharper disable once InconsistentNaming
public class ServerlessMQ : IServerlessMQ
{
    private readonly ClientWebSocketRx _client;
    private readonly ILogger? _logger;
    private readonly CancellationTokenSource _outerCancellationTokenSource = new();
    private readonly CompositeDisposable _disposables = new();
    private bool _hasConnectedBefore = false;

    public ServerlessMQ(
        string url,
        string token,
        ILogger? logger = null,
        Dictionary<string, string>? headers = null
    )
    {
        _logger = logger;

        headers ??= new();

        _client = new()
        {
            Headers = headers.Append(new("Authorization", $"Bearer {token}")).ToDictionary(),
            TlsProtocolType = SslProtocols.Tls13,
        };

        IDisposable isConnectedDisposable = _client
            .IsConnectedObservable.Do(isConnected =>
            {
                _logger?.LogDebug($"Is connected: {isConnected}");
                if (isConnected)
                {
                    var connectionInfo = new ConnectionInfo
                    {
                        IsReconnect = _hasConnectedBefore,
                        Timestamp = DateTimeOffset.Now,
                    };
                    _hasConnectedBefore = true;
                    OnConnected?.Invoke(connectionInfo);
                }
                else
                {
                    var disconnectionInfo = new DisconnectionInfo
                    {
                        Type = DisconnectionType.Lost,
                        Timestamp = DateTimeOffset.Now,
                        Reason = "Connection lost or closed",
                    };
                    OnClosed?.Invoke(disconnectionInfo);
                }
            })
            .Subscribe();
        _disposables.Add(isConnectedDisposable);

        Func<IObservable<(IDataframe dataframe, ConnectionStatus state)>> connect = () =>
            _client.WebsocketConnectWithStatusObservable(
                uri: new(url),
                hasClientPing: true,
                clientPingInterval: TimeSpan.FromSeconds(10),
                clientPingMessage: "ping message",
                cancellationToken: _outerCancellationTokenSource.Token
            )!;

        IDisposable disposableConnectionStatus = Observable
            .Defer(connect)
            .Retry()
            .Repeat()
            .DelaySubscription(TimeSpan.FromSeconds(10))
            .Do(async tuple =>
            {
                _logger?.LogDebug($"Connection status: {tuple.state}");

                if (
                    tuple.state == ConnectionStatus.DataframeReceived
                    && tuple.dataframe?.Message is { } message
                )
                {
                    _logger?.LogDebug($"Received message: {message}");
                    try
                    {
                        await ProcessMessageAsync(message);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Error processing message");
                    }
                }
            })
            .Subscribe(
                _ => { },
                ex =>
                {
                    _logger?.LogError(ex, "Connection status error");

                    // 根据异常类型推断断开原因
                    var disconnectionInfo = new DisconnectionInfo
                    {
                        Timestamp = DateTimeOffset.Now,
                        Exception = ex,
                    };

                    if (ex is OperationCanceledException)
                    {
                        disconnectionInfo.Type = DisconnectionType.ByUser;
                        disconnectionInfo.Reason = "Operation cancelled by user";
                    }
                    else if (ex is SocketException socketEx)
                    {
                        disconnectionInfo.Type = DisconnectionType.Error;
                        disconnectionInfo.Reason = $"Socket error: {socketEx.Message}";
                    }
                    else if (ex is TimeoutException)
                    {
                        disconnectionInfo.Type = DisconnectionType.Timeout;
                        disconnectionInfo.Reason = "Connection timeout";
                    }
                    else
                    {
                        disconnectionInfo.Type = DisconnectionType.Error;
                        disconnectionInfo.Reason = $"Connection error: {ex.Message}";
                    }

                    OnClosed?.Invoke(disconnectionInfo);
                },
                () => _logger?.LogDebug("Connection status completed")
            );

        _disposables.Add(disposableConnectionStatus);
    }

    private async ValueTask ProcessMessageAsync(string msg)
    {
        var jsonObject = JObject.Parse(msg);
        if (jsonObject.TryGetValue("type", out var pktTypeObj))
        {
            switch (pktTypeObj.Value<string>())
            {
                case "active_clients_change":
                    {
                        var data = jsonObject["data"]!.ToObject<ActiveBroadcastPacketData>()!;
                        await (OnClientChanged?.Invoke(data) ?? ValueTask.CompletedTask);
                    }
                    return;
                case "webhook":
                    {
                        var data = jsonObject["data"]!;
                        await (OnWebhook?.Invoke(data) ?? ValueTask.CompletedTask);
                    }
                    return;
            }
        }
        await (OnPacket?.Invoke(jsonObject) ?? ValueTask.CompletedTask);
    }

    public event Action<ConnectionInfo>? OnConnected;
    public event Action<DisconnectionInfo>? OnClosed;
    public event Func<JToken, ValueTask>? OnWebhook;
    public event Func<ActiveBroadcastPacketData, ValueTask>? OnClientChanged;
    public event Func<JToken, ValueTask>? OnPacket;

    public void Send(string msg)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            _ = _client.Sender?.SendBinary(bytes, OpcodeKind.Text);
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
            _outerCancellationTokenSource.Cancel();
            _disposables.Dispose();
            _outerCancellationTokenSource.Dispose();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error disposing ServerlessMQ");
        }
    }
}
