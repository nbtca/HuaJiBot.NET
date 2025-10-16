using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Websocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.UnitTest;

internal class WebsocketTest
{
    private Lazy<ServerlessMQ> _mq = new(() =>
        new(
            "wss://mq.nbtca.space/github",
            "nbtca_github_17126334-0997-4ba7-bdfc-4ab505064ea5",
            //LoggerFactory
            //    .Create(builder =>
            //    {
            //        builder.AddConsole().SetMinimumLevel(LogLevel.Debug); // 控制台输出最低级别
            //    })
            //    .CreateLogger(nameof(WebsocketTest))
            new ConsoleLogger()
        )
    );

    private ServerlessMQ MQ => _mq.Value;

    [Test]
    public async Task TestClientChanged()
    {
        TaskCompletionSource<ActiveBroadcastPacketData> tcs = new();
        MQ.OnClientChanged += (e) =>
        {
            tcs.SetResult(e);
            return ValueTask.CompletedTask;
        };
        var result = await tcs.Task;
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [Test]
    public async Task TestWebhook()
    {
        TaskCompletionSource<JToken> tcs = new();
        MQ.OnWebhook += e =>
        {
            tcs.SetResult(e);
            return ValueTask.CompletedTask;
        };
        var result = await tcs.Task;
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [Test]
    public async Task TestPacket()
    {
        TaskCompletionSource<JToken> tcs = new();
        MQ.OnPacket += (e) =>
        {
            tcs.SetResult(e);
            return ValueTask.CompletedTask;
        };
        var result = await tcs.Task;
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
