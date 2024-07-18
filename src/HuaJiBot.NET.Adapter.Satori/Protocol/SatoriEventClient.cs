using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using HuaJiBot.NET.Adapter.Satori.Protocol.Elements;
using HuaJiBot.NET.Adapter.Satori.Protocol.Events;
using HuaJiBot.NET.Commands;
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
        new() { ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() } };
    private readonly WebsocketClient _client;
    private readonly Timer _pingTimer;
    private readonly SatoriAdapter _service;

    public Task ConnectAsync() => _client.Start();

    public SatoriEventClient(SatoriAdapter service, Uri wsUrl, string token)
    {
        _client = new WebsocketClient(wsUrl)
        {
            IsTextMessageConversionEnabled = true,
            MessageEncoding = Encoding.UTF8,
            ReconnectTimeout = null
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

    private Task ProcessMessageAsync(string message)
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
                        JsonSerializer.CreateDefault(_jsonSerializerSettings)
                    )!;
                    if (
                        eventBody is
                        {
                            Type: SatoriEventTypes.MessageCreated,
                            SelfId: var self,
                            Channel: { Id: var groupId, Name: var groupName, },
                            User: { Id: var senderId, Name: var nickName },
                            Member: { Name: var memberName, Nick: var memberNickName },
                            Message: { } msg
                        }
                    )
                    {
                        //自身消息
                        if (self == senderId) break;
                        var name = memberNickName ?? memberName ?? nickName;
                        var messages = ElementSerializer.Deserialize(msg.Content);
                        IEnumerable<CommonCommandReader.ReaderEntity> Parse()
                        {
                            foreach (var element in messages)
                            {
                                switch (element)
                                {
                                    case TextElement { Text: var text }:
                                        yield return text;
                                        break;
                                    case AtElement { Id: var id, Name: var targetName }:
                                        yield return new CommonCommandReader.ReaderAt(
                                            id ?? "-1",
                                            targetName
                                        );
                                        break;
                                    default:
                                        _service.LogDebug($"未处理的消息元素：{element}");
                                        break;
                                        //case SharpElement: break;
                                        //case LinkElement: break;
                                        //case ImageElement: break;
                                        //case AudioElement: break;
                                        //case VideoElement: break;
                                        //case FileElement: break;
                                        //case BoldElement: break;
                                        //case ItalicElement: break;
                                        //case UnderlineElement: break;
                                        //case DeleteElement: break;
                                        //case SpoilerElement: break;
                                        //case CodeElement: break;
                                        //case SuperscriptElement: break;
                                        //case SubscriptElement: break;
                                        //case BreakElement: break;
                                        //case ParagraphElement: break;
                                        //case MessageElement: break;
                                        //case QuoteElement: break;
                                        //case AuthorElement: break;
                                }
                            }
                        }
                        NET.Events
                            .Events
                            .CallOnGroupMessageReceived(
                                new GroupMessageEventArgs(
                                    () => new DefaultCommandReader(Parse()),
                                    () => ValueTask.FromResult(groupName ?? string.Empty)
                                )
                                {
                                    RobotId = self,
                                    MessageId = msg.Id,
                                    GroupId = groupId,
                                    SenderId = senderId,
                                    SenderMemberCard = name ?? string.Empty,
                                    TextMessageLazy = new Lazy<string>(() => msg.Content),
                                    Service = _service
                                }
                            );
                    }
                    break;
                case SignalOperation.Ready:
                    var readyBody = json["body"]!.ToObject<ReadySignalBody>(
                        JsonSerializer.CreateDefault(_jsonSerializerSettings)
                    )!;

                    _service.Accounts = (from x in readyBody.Logins select x.User!.Id).ToArray();
                    var account = readyBody.Logins.First();
                    var appName = account.Platform ?? "unknown";
                    NET.Events
                        .Events
                        .CallOnBotLogin(
                            new BotLoginEventArgs
                            {
                                Accounts = _service.AllRobots,
                                ClientName = appName,
                                ClientVersion = null,
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

        return Task.CompletedTask;
    }

    private void SendSignal<T>(T signal)
        where T : Signal
    {
        var text = JsonConvert.SerializeObject(signal, _jsonSerializerSettings);
        _service.LogDebug($"WebSocket::SendSignal {text}");
        _client.Send(text);
    }
}
