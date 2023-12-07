using System.Net.WebSockets;
using System.Text;
using HuaJiBot.NET.Plugin.GitHubBridge.Types;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;
using Newtonsoft.Json;
using Websocket.Client;

namespace HuaJiBot.NET.Plugin.GitHubBridge;

public class PluginConfig : ConfigBase
{
    public string Address { get; set; } = "ws://localhost:8080";
    public Dictionary<string, string> BroadcastMap { get; set; } = new();
    public Dictionary<string, string[]> BroadcastGroup { get; set; } =
        new() { ["default"] = new[] { "123456789" } }; //默认的广播目标
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();

    private IEnumerable<string> GetBroadcastTargets(string fullName)
    {
        if (!Config.BroadcastMap.TryGetValue(fullName, out var group))
        {
            group = "default";
            Config.BroadcastMap.Add(fullName, group);
            Service.Config.Save();
        }
        if (!Config.BroadcastGroup.TryGetValue(group, out var targets))
        {
            targets = Array.Empty<string>();
            Config.BroadcastGroup.Add(group, targets);
            Service.Config.Save();
        }
        return targets;
    }

    //处理消息
    private void ProcessMessage(string msg)
    {
        var e = JsonConvert.DeserializeObject<Event>(msg)!;
        {
            const int maxChangeCount = 3;
            if (e.Body is PushEventBody body)
            {
                var sb = new StringBuilder();
                var repositoryFullName = body.Repository.FullName;
                {
                    var branch = body.Ref.Split('/').Last();
                    var mainBranch = body.Repository.MasterBranch;
                    var repoInfo = body.Repository.Name;
                    if (branch != mainBranch) //如果不是主分支，加上分支名
                        repoInfo += ":" + branch;
                    var commitCount = body.Commits.Length;
                    if (commitCount > 1)
                        sb.AppendLine($"仓库 {repoInfo} 有 {commitCount} 个新的提交： ");
                    else
                        sb.AppendLine($"仓库 {repoInfo} 有新的提交：");
                }
                if (body.Commits.Length == 0)
                {
                    sb.AppendLine("但是没有文件变更。");
                    return;
                }
                bool showFiles = body.Commits.Length == 1; //如果只有一个提交，显示文件变更
                foreach (var commit in body.Commits)
                {
                    var name = commit.Author.Username;
                    if (name == "dependabot[bot]")
                    {
                        sb.AppendLine($"- 依赖更新[Bot]：{commit.Message}");
                        continue;
                    }
                    var message = commit.Message;
                    var url = commit.Url;
                    sb.AppendLine($"{message} by @{name}");
                    if (showFiles)
                    {
                        if (commit.Added.Length > 0)
                        {
                            sb.AppendLine($"- {commit.Added.Length} 个文件新增");
                            foreach (var added in commit.Added.Take(maxChangeCount))
                                sb.AppendLine($"  {added}");
                            if (commit.Added.Length > maxChangeCount)
                                sb.AppendLine("  ...");
                        }

                        if (commit.Removed.Length > 0)
                        {
                            sb.AppendLine($"- {commit.Removed.Length} 个文件移除");
                            foreach (var removed in commit.Removed.Take(maxChangeCount))
                                sb.AppendLine($"  {removed}");
                            if (commit.Removed.Length > maxChangeCount)
                                sb.AppendLine("  ...");
                        }

                        if (commit.Modified.Length > 0)
                        {
                            sb.AppendLine($"- {commit.Modified.Length} 个文件修改");
                            foreach (var modified in commit.Modified.Take(maxChangeCount))
                                sb.AppendLine($"  {modified}");
                            if (commit.Modified.Length > maxChangeCount)
                                sb.AppendLine("  ...");
                        }
                        sb.AppendLine($"{url}");
                    }
                    else
                    {
                        //如果有多个提交，简要显示文件变更
                        sb.AppendLine(
                            $"- {commit.Added.Length} 增，{commit.Removed.Length} 减，{commit.Modified.Length} 改"
                        );
                    }
                }
                var m = sb.ToString();
                foreach (var group in GetBroadcastTargets(repositoryFullName))
                {
                    Service.SendGroupMessage(null, group, m);
                }
            }
        }
        {
            if (e.Body is WorkflowRunEventBody body) { }
        }
        {
            if (e.Body is UnknownEventBody)
            {
                Service.Log("[GitHub Bridge] 收到未实现的事件！" + e.Headers.XGithubEvent[0]);
            }
        }
    }

    //初始化
    protected override async Task Initialize()
    {
        WebsocketClient client =
            new(
                new Uri(Config.Address),
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
        client.MessageReceived.Subscribe(msg =>
        {
            if (msg.MessageType == WebSocketMessageType.Text)
            {
                try
                {
                    ProcessMessage(msg.Text ?? throw new NullReferenceException("msg.Text"));
                }
                catch (Exception e)
                {
                    Service.LogError("[GitHub Bridge] 处理消息时出现异常：", e);
                }
            }
            else
            {
                Service.Log("[GitHub Bridge] 收到非文本消息！");
            }
        });
        client.DisconnectionHappened.Subscribe(
            info => Service.Log("[GitHub Bridge] Disconnection Happened " + info.Type)
        );
        client.ReconnectionHappened.Subscribe(
            info => Service.Log("[GitHub Bridge] Reconnection Happened " + info.Type)
        );
        await client.Start();
        Service.Log("[GitHub Bridge] 启动成功！");
    }

    protected override void Unload() { }
}
