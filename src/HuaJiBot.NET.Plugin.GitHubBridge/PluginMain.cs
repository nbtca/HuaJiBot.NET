using System.Net.WebSockets;
using System.Text;
using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;
using HuaJiBot.NET.Plugin.GitHubBridge.Types;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HuaJiBot.NET.Plugin.GitHubBridge;

public class PluginConfig : ConfigBase
{
    public string ShortLinkApi { get; set; } = "https://link.nbtca.space/api/shorten";
    public string ShortLinkToken { get; set; } = "";
    public string Address { get; set; } = "ws://localhost:8080";
    public string AuthBearer { get; set; } = "";
    public Dictionary<string, string> BroadcastMap { get; set; } = new();
    public Dictionary<string, string[]> BroadcastGroup { get; set; } =
        new() { ["default"] = ["123456789"] }; //默认的广播目标
}

public partial class PluginMain
{
    internal ShortLinkApi ShortLinkApi = null!;

    internal IEnumerable<string> GetBroadcastTargets(string fullName)
    {
        if (!Config.BroadcastMap.TryGetValue(fullName, out var group))
        {
            group = "default";
            Config.BroadcastMap.Add(fullName, group);
            Service.Config.Save();
        }
        if (!Config.BroadcastGroup.TryGetValue(group, out var targets))
        {
            targets = [];
            Config.BroadcastGroup.Add(group, targets);
            Service.Config.Save();
        }
        return targets;
    }
}

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();

    //处理消息
    private async Task ProcessMessageAsync(string msg)
    {
        var jsonObject = JObject.Parse(msg);
        if (jsonObject.TryGetValue("type", out var pktTypeObj))
        {
            var pktType = pktTypeObj.Value<string>();
            if (pktType == "active_clients_change")
            {
                var data = jsonObject["data"]!.Value<JArray>("clients")!;
                var clients = data.Select(x =>
                        x.Value<string>("address")
                        + "("
                        + (x["headers"]?["Cf-Ipcountry"]?[0] ?? "?")
                        + ":"
                        + (x["headers"]?["X-Forwarded-For"]?[0] ?? "?")
                        + ")"
                    )
                    .ToArray();
                Info("当前在线客户端：" + string.Join(", ", clients));
            }
            else if (pktType == "webhook")
            {
                var e = jsonObject["data"]!.ToObject<Event>()!;
                {
                    switch (e.Body)
                    {
                        case PushEventBody body: //推送事件
                            await this.DispatchPushEventAsync(body);
                            break;
                        case IssuesEventBody body: //issue事件
                            await this.DispatchIssuesEventAsync(body);
                            break;
                        case IssueCommentEventBody body: //issue comment事件
                            await this.DispatchIssueCommentEventAsync(body);
                            break;
                        case WorkflowRunEventBody body: //actions构建事件
                            await this.DispatchWorkflowRunEventAsync(body);
                            break;
                        case UnknownEventBody body:
                            Info("收到未实现的事件！" + e.Headers.XGithubEvent[0]);
                            break;
                    }
                }
            }
            return;
        }
    }

    //初始化
    protected override async Task InitializeAsync()
    {
        ShortLinkApi = new ShortLinkApi(Config.ShortLinkToken);
        WebsocketClient client = new(
            new Uri(Config.Address),
            () =>
            {
                var client = new ClientWebSocket
                {
                    Options = { CollectHttpResponseDetails = true },
                };
                client.Options.SetRequestHeader("Authorization", $"Bearer {Config.AuthBearer}");
                return client;
            }
        )
        {
            IsReconnectionEnabled = true,
            ReconnectTimeout = null,
            MessageEncoding = Encoding.UTF8,
            IsTextMessageConversionEnabled = true,
        };
        client.MessageReceived.Subscribe(msg =>
        {
            if (msg.MessageType == WebSocketMessageType.Text)
            {
                try
                {
                    ProcessMessageAsync(msg.Text ?? throw new NullReferenceException("msg.Text"))
                        .ContinueWith(
                            task =>
                            {
                                var ex = task.Exception;
                                if (ex is not null)
                                    Error("ProcessMessage 处理消息时出现异常：", ex);
                                if (msg.Text is { } raw)
                                    Error("ProcessMessage 处理消息时出现异常Raw：", raw);
                            },
                            TaskContinuationOptions.OnlyOnFaulted
                        );
                }
                catch (Exception e)
                {
                    Error("处理消息时出现异常：", e);
                    if (msg.Text is { } raw)
                        Error("处理消息时出现异常Raw：", raw);
                }
            }
            else
            {
                Info("收到非文本消息！");
            }
        });
        client.DisconnectionHappened.Subscribe(info =>
            Info(
                "Disconnection Happened. Type:"
                    + info.Type
                    + " Description:"
                    + info.CloseStatusDescription
            )
        );
        client.ReconnectionHappened.Subscribe(info => Info("Reconnection Happened " + info.Type));
        await client.Start();
        Info("启动成功！");
    }

    protected override void Unload() { }
}
