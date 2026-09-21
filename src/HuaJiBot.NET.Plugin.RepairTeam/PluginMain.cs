using System.Text;
using HuaJiBot.NET.Interfaces;
using Newtonsoft.Json;
using HuaJiBot.NET.Utils;

namespace HuaJiBot.NET.Plugin.RepairTeam;

public class PluginConfig : ConfigBase
{
    public string NsqUrl = "";
    public string NsqSecret = "";
    public string NsqTopic = "";
    public string NsqChannel = "";
    public string? PushRawGroup = "";
    public string[] PushInfoGroup = [];
}

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    private NsqConnector? _nsq;

    protected override void Initialize()
    {
        if (string.IsNullOrWhiteSpace(Config.NsqUrl) || string.IsNullOrWhiteSpace(Config.NsqTopic))
        {
            Service.Warn("[RepairTeam] 未配置 NsqUrl 或 NsqTopic，不接收维修事件");
            return;
        }
        _nsq = new(Config.NsqUrl, Config.NsqTopic, Config.NsqChannel, Config.NsqSecret);
        Service.Log("[RepairTem] 启动成功！");
        _nsq.MessageReceived += OnMessageReceived;
    }

    internal class LogEventEntity
    {
        [JsonProperty("action")]
        public string? Action { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("event_id")]
        public long EventId { get; set; }

        [JsonProperty("gmt_create")]
        public DateTimeOffset GmtCreate { get; set; }

        [JsonProperty("member_alias")]
        public string? MemberAlias { get; set; }

        [JsonProperty("member_id")]
        public string? MemberId { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("problem")]
        public string? Problem { get; set; }
    }

    // Actions are defined in Saturday util/event-action.go.
    private static string ActionText(string? action) =>
        action switch
        {
            "create" => "新报修",
            "accept" => "已接单",
            "cancel" => "报修人已取消",
            "drop" => "队员已放弃，重新待接单",
            "commit" => "维修完成，待审核",
            "alterCommit" => "维修记录已修改，待审核",
            "reject" => "审核退回，需重新处理",
            "close" => "审核通过，已结单",
            "update" => "报修信息已更新",
            _ => action ?? "未知动作",
        };

    internal static string FormatEvent(LogEventEntity e)
    {
        var s = new StringBuilder();
        s.AppendLine($"【维修 #{e.EventId}】{ActionText(e.Action)}");
        var time = e.GmtCreate.ToOffset(NetworkTime.LocalTimeZoneOffset).ToString("MM-dd HH:mm");
        var member = string.IsNullOrWhiteSpace(e.MemberAlias) ? e.MemberId : e.MemberAlias;
        s.AppendLine(string.IsNullOrWhiteSpace(member) ? time : $"{member} · {time}");
        if (!string.IsNullOrWhiteSpace(e.Model))
            s.AppendLine($"机型：{e.Model}");
        if (!string.IsNullOrWhiteSpace(e.Problem))
            s.AppendLine($"问题：{e.Problem}");
        if (!string.IsNullOrWhiteSpace(e.Description))
            s.AppendLine($"说明：{e.Description}");
        return s.ToString().TrimEnd();
    }

    private void OnMessageReceived(object? sender, string msg)
    {
        Service.Log(msg);
        if (!string.IsNullOrWhiteSpace(Config.PushRawGroup))
            _ = Service.TrySendGroupMessageAsync(Config.PushRawGroup, msg);
        if (Config.PushInfoGroup.Length != 0)
        {
            LogEventEntity e;
            try
            {
                e = JsonConvert.DeserializeObject<LogEventEntity>(msg)!;
            }
            catch (JsonException ex)
            {
                Service.LogError("[RepairTeam] 无法解析维修事件", ex);
                return;
            }
            var str = FormatEvent(e);
            foreach (var group in Config.PushInfoGroup)
                _ = Service.TrySendGroupMessageAsync(group, str);
        }
    }

    protected override void Unload() { }

    public PluginConfig Config { get; } = new();
}
