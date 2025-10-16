using HuaJiBot.NET.Logger;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Websocket;

// ReSharper disable once InconsistentNaming
public class ServerlessMQ : IServerlessMQ
{
    private readonly WebsocketClient _client;

    public ServerlessMQ(
        string url,
        string token,
        ILogger? logger = null,
        Dictionary<string, string>? headers = null
    )
    {
        _client = new WebsocketClient(url, token, logger, headers);
        _client.OnMessage += ProcessMessageAsync;
        _client.OnConnected += info => OnConnected?.Invoke(info);
        _client.OnClosed += info => OnClosed?.Invoke(info);
    }

    private async ValueTask ProcessMessageAsync(JToken msg)
    {
        if (msg is JObject jsonObject && jsonObject.TryGetValue("type", out var pktTypeObj))
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
        await (OnPacket?.Invoke(msg) ?? ValueTask.CompletedTask);
    }

    public event Func<JToken, ValueTask>? OnMessage
    {
        add => _client.OnMessage += value;
        remove => _client.OnMessage -= value;
    }

    public event Action<ConnectionInfo>? OnConnected;
    public event Action<DisconnectionInfo>? OnClosed;
    public event Func<JToken, ValueTask>? OnWebhook;
    public event Func<ActiveBroadcastPacketData, ValueTask>? OnClientChanged;
    public event Func<JToken, ValueTask>? OnPacket;

    public void Send(string msg) => _client.Send(msg);

    public void Dispose() => _client.Dispose();
}
