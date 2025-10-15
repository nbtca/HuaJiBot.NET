using HuaJiBot.NET.Websocket;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot;

internal class ForwardWebSocketClient
{
    private readonly OneBotMessageHandler _handler;
    public string? QQ => _handler.QQ;
    private readonly WebsocketClient _client;
    internal readonly OneBotApi Api;

    public ValueTask ConnectAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ForwardWebSocketClient(OneBotAdapter service, string wsUrl, string? token)
    {
        void Send(string text) => _client!.Send(text);
        Api = new(service, Send);
        _handler = new(Api, service);
        _client = new(wsUrl, token, service.Logger);
        _client.OnMessage += async msg =>
        {
            try
            {
                await _handler.ProcessMessageAsync((JObject)msg);
            }
            catch (Exception e)
            {
                service.LogError("[OneBotWsClient] 处理消息时出现异常：", e);
            }
        };
        _client.OnConnected += info =>
        {
            service.Log("[OneBotWsClient] Connection Happened.");
        };
        _client.OnClosed += e =>
        {
            service.Log(
                "[OneBotWsClient] Disconnection Happened. Type:"
                    + e.Type
                    + " Description:"
                    + e.Reason
            );
        };
    }
}
