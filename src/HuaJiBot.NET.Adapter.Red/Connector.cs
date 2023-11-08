using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.Adapter.Red;

internal partial class Connector(BotServiceBase api, string url, string authorizationToken)
{
    readonly Websocket.Client.WebsocketClient _client =
        new(
            new("ws://" + url),
            () =>
                new System.Net.WebSockets.ClientWebSocket()
                {
                    Options = { KeepAliveInterval = TimeSpan.FromSeconds(5), }
                }
        )
        {
            IsReconnectionEnabled = true,
        };

    //private Timer? _keepAliveTimer = new(1_0000);
    public async Task Connect()
    {
        _client.MessageReceived.Subscribe(msg =>
        {
            ProcessMessage(msg.Text);
        });
        _client.DisconnectionHappened.Subscribe(info =>
        {
            api.Warn("断开连接 原因：" + info.Type);
        });
        _client.ReconnectionHappened.Subscribe(info =>
        {
            api.Warn("建立连接：" + info.Type);
            SendConnectMsg(authorizationToken); //重连后重新发送连接消息
        });
        await _client.Start();
        api.Log("Websocket连接已建立。");
        //await SendConnectMsg(authorizationToken);
        //_keepAliveTimer.Start();
        //_keepAliveTimer.Elapsed += async (sender, args) =>
        //{
        //    await Client.SendInstant("{}");
        //};
    }

    private void SendConnectMsg(string authorizationToken)
    {
        var payload = new Payload<ConnectSend>
        {
            Type = "meta::connect",
            Data = new ConnectSend { token = authorizationToken }
        };
        var json = JsonConvert.SerializeObject(payload);
        _client.Send(json);
    }

    private void ProcessMessage(string? jsonString)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<Payload<JToken>>(jsonString!);
            switch (data!.Type)
            {
                case "meta::connect": //连接成功
                    //{
                    //  "type": "meta::connect",
                    //  "payload": {
                    //    "version": "0.0.48",
                    //    "name": "chronocat",
                    //    "authData": {
                    //      "account": "111",
                    //      "mainAccount": "",
                    //      "uin": "111",
                    //      "uid": "u_i11-1111",
                    //      "nickName": "",
                    //      "gender": 0,
                    //      "age": 0,
                    //      "faceUrl": "",
                    //      "a2": "",
                    //      "d2": "",
                    //      "d2key": ""
                    //    }
                    //  }
                    //}
                    var connect = data.Data.ToObject<ConnectRecv>()!;
                    Events.Events.CallOnBotLogin(
                        new BotLoginEventArgs
                        {
                            AccountId = connect.AuthData!.Account!,
                            ClientName = connect.Name!,
                            ClientVersion = connect.Version!,
                            Service = api
                        }
                    );
                    break;
                case "message::recv":
                    api.LogDebug(data.Data);
                    foreach (var msg in data.Data.ToObject<MessageRecv[]>()!)
                    {
                        Events.Events.CallOnGroupMessageReceived(
                            new GroupMessageEventArgs
                            {
                                Service = api,
                                GroupId = msg.PeerUin!,
                                SenderId = msg.senderUid!,
                                GroupName = msg.peerName!,
                                SenderMemberCard = string.IsNullOrWhiteSpace(msg.sendMemberName)
                                    ? msg.sendNickName!
                                    : msg.sendMemberName!,
                                TextMessageLazy = new(() =>
                                {
                                    var sb = new StringBuilder();
                                    foreach (var element in msg.Elements)
                                    {
                                        if (element.textElement is { } text)
                                        {
                                            sb.Append(text.content);
                                        }
                                        else
                                        {
                                            sb.Append($"[消息类型：{element.elementType}]");
                                        }
                                    }
                                    return sb.ToString();
                                })
                            }
                        );
                    }
                    break;
            }
            //api.LogDebug("message received " + jsonString);
        }
        catch (Exception ex)
        {
            api.LogError(nameof(ProcessMessage), ex);
        }
    }

    public void Send(string json)
    {
        _client.Send(json);
    }
}
