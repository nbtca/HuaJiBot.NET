using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
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
    public string SaturdayApi = "https://api.nbtca.space";
    public string[] RemindGroups = [];
    public DateTimeOffset? RemindSince;
    public int RemindHour = 12;
    public double OpenDays = 1;
    public double AcceptedDays = 3;
    public double CommittedDays = 2;
}

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private NsqConnector? _nsq;
    private SaturdayClient? _saturday;
    private TicketReminder? _reminder;

    protected override void Initialize()
    {
        _saturday = new SaturdayClient(Http, Config.SaturdayApi, (msg, ex) => Service.LogError(msg, ex));
        _reminder = new TicketReminder(
            Service,
            Config,
            _saturday.GetActiveTicketsAsync,
            Path.Combine(Service.GetPluginDataPath(), "ticket_reminder.json")
        );
        _reminder.Start();
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

    private static string? StateTag(string? action) =>
        action switch
        {
            "create" => "新报修",
            "accept" => "已接单",
            "cancel" => "已取消",
            "drop" => "已放弃",
            "commit" or "alterCommit" => "待审核",
            "reject" => "已退回",
            "close" => "已结单",
            "update" => "已更新",
            _ => null,
        };

    internal static Post EventPost(LogEventEntity e)
    {
        var lines = new List<string> { $"🛠 **#{e.EventId} {Post.Escape(ActionText(e.Action))}**" };
        var device = string.Join(
            " · ",
            new[] { e.Model, e.Problem }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Post.Escape(x!))
        );
        if (device.Length > 0)
            lines.Add(device);
        if (!string.IsNullOrWhiteSpace(e.Description))
            lines.Add(
                "\n"
                    + string.Join(
                        "\n",
                        from line in e.Description.ReplaceLineEndings("\n").Trim().Split('\n')
                        select "> " + Post.Escape(line)
                    )
            );
        var time = e.GmtCreate.ToOffset(NetworkTime.LocalTimeZoneOffset).ToString("MM-dd HH:mm");
        var member = string.IsNullOrWhiteSpace(e.MemberAlias) ? e.MemberId : e.MemberAlias;
        lines.Add("\n" + (string.IsNullOrWhiteSpace(member) ? time : $"{Post.Escape(member)} · {time}"));
        return new(string.Join("\n", lines), FormatEvent(e))
        {
            Tags = StateTag(e.Action) is { } tag ? ["维修", tag] : ["维修"],
            Links = [new("去处理", TicketDigest.PortalUrl)],
        };
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
            var post = EventPost(e);
            foreach (var group in Config.PushInfoGroup)
                _ = Service.TrySendGroupMessageAsync(group, post);
        }
    }

    [Command("工单", "查看进行中的维修工单")]
    // ReSharper disable once UnusedMember.Local
    private async Task TicketsCommandAsync(GroupMessageEventArgs e)
    {
        if (!Config.RemindGroups.Contains(e.GroupId) || _saturday is null)
            return;
        if (Config.RemindSince is not { } since)
        {
            await e.Reply("未配置 RemindSince，工单查询未启用");
            return;
        }
        List<Ticket> tickets;
        try
        {
            tickets = await _saturday.GetActiveTicketsAsync(since);
        }
        catch (Exception ex)
        {
            Service.LogError("[RepairTeam] 读取维修工单失败", ex);
            await e.Reply("维修系统暂时无法访问");
            return;
        }
        await e.Reply(
            tickets.Count == 0
                ? "没有进行中的工单"
                : TicketDigest.Format($"【维修工单】进行中 {tickets.Count} 张", tickets, NetworkTime.Now)
        );
    }

    protected override void Unload() => _reminder?.Dispose();

    public PluginConfig Config { get; } = new();
}
