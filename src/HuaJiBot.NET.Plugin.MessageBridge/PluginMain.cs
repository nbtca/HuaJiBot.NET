using System.Net.WebSockets;
using System.Text;
using Websocket.Client;

namespace HuaJiBot.NET.Plugin.MessageBridge;

public class PluginConfig : ConfigBase
{
    public ClientInfo[] Clients { get; set; } = Array.Empty<ClientInfo>();

    public class ClientInfo
    {
        public string Address { get; set; } = "ws://localhost:8080";
        public string[] Groups { get; set; } = Array.Empty<string>();
    }
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();

    private List<(PluginConfig.ClientInfo info, WebsocketClient client)> _clients = new();

    //初始化
    protected override async Task InitializeAsync()
    {
        foreach (var clientInfo in Config.Clients)
        {
            WebsocketClient client =
                new(
                    new Uri(clientInfo.Address),
                    () =>
                        new ClientWebSocket
                        {
                            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
                        }
                )
                {
                    IsReconnectionEnabled = true,
                    ReconnectTimeout = null,
                    MessageEncoding = Encoding.UTF8,
                    IsTextMessageConversionEnabled = true
                };
            client
                .MessageReceived
                .Subscribe(msg =>
                {
                    if (msg.MessageType == WebSocketMessageType.Text)
                    {
                        try
                        {
                            ProcessMessage(
                                msg.Text ?? throw new NullReferenceException("msg.Text"),
                                clientInfo
                            );
                        }
                        catch (Exception e)
                        {
                            Error("处理消息时出现异常：", e);
                        }
                    }
                    else
                    {
                        Info("收到非文本消息！");
                    }
                });
            client
                .DisconnectionHappened
                .Subscribe(info => Info("Disconnection Happened " + info.Type));
            client
                .ReconnectionHappened
                .Subscribe(info => Info("Reconnection Happened " + info.Type));
            await client.Start();
            _clients.Add((clientInfo, client));
        }

        Info("启动成功！");
    }

    private void ProcessMessage(string messageRaw, PluginConfig.ClientInfo clientInfo) { }

    protected override void Unload() { }
}
