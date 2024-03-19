using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using HuaJiBot.NET.Bot;
using Websocket.Client;

namespace HuaJiBot.NET.Adapter.OneBot;

internal class ForwardWebSocketClient
{
    private readonly OneBotMessageHandler _handler;
    public string? QQ => _handler.QQ;
    private readonly WebsocketClient _client;

    public Task ConnectAsync() => _client.Start();

    public ForwardWebSocketClient(BotServiceBase service, string wsUrl, string? token)
    {
        _handler = new OneBotMessageHandler(service, text => _client!.Send(text));
        _client = new WebsocketClient(
            new Uri(wsUrl),
            () =>
            {
                var cfg = new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval = TimeSpan.FromSeconds(5),
                        CollectHttpResponseDetails = true,
                        //Credentials = new NetworkCredential("Bearer", token),
                    }
                };
                if (!string.IsNullOrEmpty(token))
                    cfg.Options.SetRequestHeader("Authorization", "Bearer " + token);
                return cfg;
            }
        )
        {
            IsReconnectionEnabled = true,
            ReconnectTimeout = TimeSpan.FromSeconds(1),
            MessageEncoding = Encoding.UTF8,
            IsTextMessageConversionEnabled = true,
        };
        _client
            .MessageReceived
            .Where(m => m.MessageType == WebSocketMessageType.Text)
            .Select(m => m.Text)
            .Subscribe(msg =>
            {
                try
                {
                    _handler
                        .ProcessMessageAsync(msg ?? throw new NullReferenceException("msg.Text"))
                        .ContinueWith(
                            task =>
                            {
                                var ex = task.Exception;
                                if (ex is not null)
                                    service.LogError(
                                        "[OneBotWsClient] ProcessMessage 处理消息时出现异常：",
                                        ex
                                    );
                            },
                            TaskContinuationOptions.OnlyOnFaulted
                        );
                }
                catch (Exception e)
                {
                    service.LogError("[OneBotWsClient] 处理消息时出现异常：", e);
                }
            });
        _client
            .DisconnectionHappened
            .Subscribe(
                info =>
                    service.Log(
                        "[OneBotWsClient] Disconnection Happened. Type:"
                            + info.Type
                            + " Description:"
                            + info.CloseStatusDescription
                    )
            );
        _client
            .ReconnectionHappened
            .Subscribe(info => service.Log("[OneBotWsClient] Reconnection Happened " + info.Type));
        //var timer = new System.Timers.Timer(500);
        //timer.Elapsed += (sender, args) =>
        //{ //send ping
        //    _client.Send("{}");
        //};
        //timer.Start();
    }
}
