using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        );

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
        });
        Client.ErrorReconnectTimeout =
            Client.ReconnectTimeout =
            Client.LostReconnectTimeout =
                TimeSpan.FromSeconds(10);
        await Client.Start();
        Console.WriteLine("Connect to Red");
        await SendConnectMsg(authorizationToken);
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
        var data = JsonConvert.DeserializeObject<Payload<JToken>>(jsonString);
        switch (data.Type)
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
                Console.WriteLine(
                    $"已链接到{connect.Name}@{connect.Version} 账号{connect.AuthData!.Account}"
                );
                break;
        }
        Console.WriteLine("message received " + jsonString);
    }
}
