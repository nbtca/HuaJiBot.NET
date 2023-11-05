using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using HuaJiBot.NET.Events;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Adapter.Red;

internal class Connector(string url)
{
    readonly Websocket.Client.WebsocketClient Client =
        new(
            new(url),
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
    public async Task Connect(string authorizationToken)
    {
        Client.MessageReceived.Subscribe(msg =>
        {
            ProcessMessage(msg.Text);
        });
        Client.DisconnectionHappened.Subscribe(info =>
        {
            Console.WriteLine("disconnected " + info.Type);
        });
        Client.ReconnectionHappened.Subscribe(info =>
        {
            Console.WriteLine("reconnected " + info.Type);
            //_ = SendConnectMsg(authorizationToken);
        });
        await Client.Start();
        Console.WriteLine("Connect to Red.");
        await SendConnectMsg(authorizationToken);
        //_keepAliveTimer.Start();
        //_keepAliveTimer.Elapsed += async (sender, args) =>
        //{
        //    await Client.SendInstant("{}");
        //};
    }

    private async Task SendConnectMsg(string authorizationToken)
    {
        var payload = new Payload<ConnectSend>
        {
            Type = "meta::connect",
            Data = new ConnectSend { token = authorizationToken }
        };
        var json = JsonConvert.SerializeObject(payload);
        await Client.SendInstant(json);
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
                            ClientVersion = connect.Version!
                        }
                    );
                    break;
                case "message::recv":
                    foreach (var msg in data.Data.ToObject<MessageRecv[]>()!)
                    {
                        Events.Events.CallOnGroupMessageReceived(
                            new GroupMessageEventArgs
                            {
                                GroupId = msg.GroupId!,
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
                                            sb.Append(text);
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
            Console.WriteLine("message received " + jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
