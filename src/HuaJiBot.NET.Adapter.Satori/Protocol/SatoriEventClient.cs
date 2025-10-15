using HuaJiBot.NET.Adapter.Satori.Protocol.Elements;
using HuaJiBot.NET.Adapter.Satori.Protocol.Events;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Websocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Adapter.Satori.Protocol;

internal class SatoriEventClient
{
    private readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy(),
        },
    };
    private readonly WebsocketClient _client;
    private readonly Timer _pingTimer;
    private readonly SatoriAdapter _service;

    public Task ConnectAsync() => Task.CompletedTask;

    public SatoriEventClient(SatoriAdapter service, Uri wsUrl, string token)
    {
        _client = new(wsUrl);
        _service = service;

        // 订阅消息接收事件
        _client.OnMessage += async msg =>
        {
            try
            {
                await ProcessMessageAsync(msg ?? throw new NullReferenceException("msg.Text"));
            }
            catch (Exception ex)
            {
                service.LogError("[SatoriEventClient] ProcessMessage 处理消息时出现异常：", ex);
            }
        };
        // 订阅断开连接事件
        _client.OnClosed += info =>
        {
            service.Log(
                "[SatoriEventClient] Disconnection Happened. Type:"
                    + info.Type
                    + " Description:"
                    + info.Reason
            );
            _pingTimer?.Stop();
        };

        // 订阅重连事件
        _client.OnConnected += info =>
        {
            service.Log("[SatoriEventClient] Reconnection Happened " + info.IsReconnect);
            var identify = new Signal<IdentifySignalBody> //鉴权
            {
                Op = SignalOperation.Identify,
                Body = new() { Token = token },
            };
            SendSignal(identify);
            _pingTimer?.Start();
        };

        _pingTimer = new()
        {
            AutoReset = true,
            Interval = TimeSpan.FromSeconds(10).TotalMilliseconds,
        };
        _pingTimer.Elapsed += (_, _) => SendSignal(new Signal { Op = SignalOperation.Ping });
    }

    private ValueTask ProcessMessageAsync(JToken json)
    {
        try
        {
            //_service.LogDebug($"WebSocket::Process {message}");
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
                            Channel: { Id: var groupId, Name: var groupName },
                            User: { Id: var senderId, Name: var nickName },
                            Member: { Name: var memberName, Nick: var memberNickName },
                            Message: { } msg
                        }
                    )
                    {
                        //自身消息
                        if (self == senderId)
                            break;
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
                                    case QuoteElement { Id: var id, Forward: var forward } quote:
                                        quote.Attributes.TryGetValue(
                                            "chronocat:seq",
                                            out var messageSeq
                                        );
                                        string? senderId = null;
                                        string? senderName = null;
                                        string? content = null;
                                        foreach (var child in quote.ChildElements)
                                        {
                                            if (child is AuthorElement author)
                                            {
                                                senderId = author.UserId;
                                                senderName = author.Nickname;
                                                if (senderId is null)
                                                    author.Attributes.TryGetValue(
                                                        "id",
                                                        out senderId
                                                    );
                                                if (senderName is null)
                                                    author.Attributes.TryGetValue(
                                                        "name",
                                                        out senderId
                                                    );
                                            }
                                            else if (child is TextElement text)
                                            {
                                                content = text.Text;
                                            }
                                        }
                                        yield return new CommonCommandReader.ReaderReply(
                                            new(
                                                messageId: id,
                                                seqId: messageSeq,
                                                senderId: senderId,
                                                content: content
                                            )
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
                        _service.Events.CallOnGroupMessageReceived(
                            new(
                                () => new DefaultCommandReader(Parse()),
                                () => ValueTask.FromResult(groupName ?? string.Empty)
                            )
                            {
                                RobotId = self,
                                MessageId = msg.Id,
                                GroupId = groupId,
                                SenderId = senderId,
                                SenderMemberCard = name ?? string.Empty,
                                TextMessageLazy = new(() => msg.Content),
                                Service = _service,
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
                    _service.Log(
                        $"{appName} {account.Status} Features: {string.Join(",", account.Features)}"
                    );
                    _service.Events.CallOnBotLogin(
                        new()
                        {
                            Accounts = _service.AllRobots,
                            ClientName = appName,
                            ClientVersion = null,
                            Service = _service,
                        }
                    );
                    break;
                case SignalOperation.Pong:
                    //_service.LogDebug("WebSocket::Pong");
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

        return ValueTask.CompletedTask;
    }

    private void SendSignal<T>(T signal)
        where T : Signal
    {
        var text = JsonConvert.SerializeObject(signal, _jsonSerializerSettings);
        //_service.LogDebug($"WebSocket::SendSignal {text}");
        _client.Send(text);
    }

    public void Dispose()
    {
        _pingTimer?.Stop();
        _pingTimer?.Dispose();
        _client?.Dispose();
    }
}
