using HuaJiBot.NET.Websocket;
using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;
using HuaJiBot.NET.Plugin.GitHubBridge.Types;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;

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

    //初始化
    protected override async Task InitializeAsync()
    {
        ShortLinkApi = new ShortLinkApi(Config.ShortLinkToken);
        ServerlessMQ client = new(Config.Address, Config.AuthBearer);
        client.OnWebhook += async data =>
        {
            var e = data.ToObject<Event>()!;
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
        };
        client.OnConnected += info =>
        {
            var status = info.IsReconnect ? "重新连接成功" : "连接成功";
            Info($"{status} - 时间: {info.Timestamp:yyyy-MM-dd HH:mm:ss}");
        };
        client.OnClosed += info =>
        {
            Info(
                $"连接断开 - 类型: {info.Type}, 原因: {info.Reason ?? "未知"}, 时间: {info.Timestamp:yyyy-MM-dd HH:mm:ss}"
                    + (info.CloseStatus.HasValue ? $", 状态码: {info.CloseStatus}" : "")
            );
        };
        client.OnClientChanged += async clients =>
        {
            var data = clients.Clients;
            var clientsStr = data.Select(x =>
                    x.Address
                    + "("
                    + (x.Headers.GetValueOrDefault("Cf-Ipcountry")?[0] ?? "?")
                    + ":"
                    + (x.Headers.GetValueOrDefault("X-Forwarded-For")?[0] ?? "?")
                    + ")"
                )
                .ToArray();
            Info("当前在线客户端：" + string.Join(", ", clientsStr));
        };

        Info("启动成功！");
    }

    protected override void Unload() { }
}
