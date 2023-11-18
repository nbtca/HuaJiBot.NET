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
    public string[] BroadcastGroupId { get; set; } = Array.Empty<string>();
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();

    //处理消息
    private void ProcessMessage(string msg)
    {
        var e = JsonConvert.DeserializeObject<Event>(msg)!;
        {
            if (e.Body is PushEventBody body)
            {
                var sb = new StringBuilder();
                var repo = body.Repository.FullName;
                //var mainBranch = body.Repository.MasterBranch;
                var branch = body.Ref.Split('/').Last();
                //if (branch != mainBranch)
                sb.AppendLine($"{body.Commits.Length} new commit to {repo}:{branch}");
                foreach (var commit in body.Commits)
                {
                    var name = commit.Author.Name;
                    var message = commit.Message;
                    var url = commit.Url;
                    sb.AppendLine($"{message} by {name}");
                    sb.AppendLine($"{url}");
                }
                var m = sb.ToString();
                foreach (var group in Config.BroadcastGroupId)
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
            new(new Uri(Config.Address))
            {
                IsReconnectionEnabled = true,
                ReconnectTimeout = TimeSpan.FromSeconds(30),
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
