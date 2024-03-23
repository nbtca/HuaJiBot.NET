using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using HuaJiBot.NET.Adapter.Satori.Protocol.Events;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Websocket.Client;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Adapter.Satori.Protocol;

internal class SatoriEventClient
{
    private readonly JsonSerializerSettings _jsonSerializerSettings =
        new() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    private readonly WebsocketClient _client;
    private readonly Timer _pingTimer;
    private readonly SatoriAdapter _service;

    public Task ConnectAsync() => _client.Start();

    public SatoriEventClient(SatoriAdapter service, Uri wsUrl, string token)
    {
        _client = new WebsocketClient(wsUrl)
        {
            IsTextMessageConversionEnabled = true,
            MessageEncoding = Encoding.UTF8
        };
        _service = service;
        _client
            .MessageReceived
            .Where(m => m.MessageType == WebSocketMessageType.Text)
            .Select(m => m.Text)
            .Subscribe(msg =>
            {
                try
                {
                    ProcessMessageAsync(msg ?? throw new NullReferenceException("msg.Text"))
                        .ContinueWith(
                            task =>
                            {
                                var ex = task.Exception;
                                if (ex is not null)
                                    service.LogError(
                                        "[SatoriEventClient] ProcessMessage 处理消息时出现异常：",
                                        ex
                                    );
                            },
                            TaskContinuationOptions.OnlyOnFaulted
                        );
                }
                catch (Exception e)
                {
                    service.LogError("[SatoriEventClient] 处理消息时出现异常：", e);
                }
            });
        _client
            .DisconnectionHappened
            .Subscribe(info =>
            {
                service.Log(
                    "[SatoriEventClient] Disconnection Happened. Type:"
                        + info.Type
                        + " Description:"
                        + info.CloseStatusDescription
                );
                _pingTimer?.Stop();
            });
        _client
            .ReconnectionHappened
            .Subscribe(info =>
            {
                service.Log("[SatoriEventClient] Reconnection Happened " + info.Type);
                var identify = new Signal<IdentifySignalBody> //鉴权
                {
                    Op = SignalOperation.Identify,
                    Body = new IdentifySignalBody { Token = token }
                };
                SendSignal(identify);
                _pingTimer?.Start();
            });

        _pingTimer = new Timer
        {
            AutoReset = true,
            Interval = TimeSpan.FromSeconds(10).TotalMilliseconds
        };
        _pingTimer.Elapsed += (_, _) => SendSignal(new Signal { Op = SignalOperation.Ping });
    }

    private async Task ProcessMessageAsync(string message)
    {
        try
        {
            _service.LogDebug($"WebSocket::Process {message}");
            var json = JObject.Parse(message);
            var op = (SignalOperation)json.Value<int>("op");
            switch (op)
            {
                case SignalOperation.Event:
                    var eventBody = json["body"]!.ToObject<Event>(
                        new JsonSerializer
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    )!;
                    break;
                case SignalOperation.Ready:
                    var readyBody = json["body"]!.ToObject<ReadySignalBody>(
                        new JsonSerializer
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    )!;
                    _service.Accounts = readyBody.Logins.ToArray();
                    var account = readyBody.Logins.First();
                    var appName = account.Platform ?? "unknown";
                    NET.Events
                        .Events
                        .CallOnBotLogin(
                            _service,
                            new BotLoginEventArgs
                            {
                                Accounts = _service.GetAllRobots(),
                                ClientName = appName,
                                ClientVersion = "unknown",
                                Service = _service
                            }
                        );
                    break;
                case SignalOperation.Pong:
                    _service.LogDebug("WebSocket::Pong");
                    break;
                default:
                    _service.Log($"WebSocket::Process Unknown operation {op}");
                    break;
            }
        }
        catch (Exception e)
        {
            _service.Log(e);
        }
    }

    private void SendSignal<T>(T signal)
        where T : Signal
    {
        var text = JsonConvert.SerializeObject(signal, _jsonSerializerSettings);
        _service.LogDebug($"WebSocket::SendSignal {text}");
        _client.Send(text);
    }
}
