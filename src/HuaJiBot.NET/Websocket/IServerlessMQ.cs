using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Websocket;

// ReSharper disable once InconsistentNaming
public interface IServerlessMQ : IWebsocketClient
{
    public event Func<JToken, ValueTask>? OnWebhook;
    public event Func<ActiveBroadcastPacketData, ValueTask>? OnClientChanged;
    public event Func<JToken, ValueTask>? OnPacket;
}

public interface IWebsocketClient : IDisposable
{
    public event Func<JToken, ValueTask>? OnMessage;
    public event Action<ConnectionInfo>? OnConnected;
    public event Action<DisconnectionInfo>? OnClosed;
    public void Send(string msg);
}
