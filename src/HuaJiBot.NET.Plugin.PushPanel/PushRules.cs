using System.Globalization;
using HuaJiBot.NET.Config;
using HuaJiBot.NET.Plugin.Calendar;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using BridgeConfig = HuaJiBot.NET.Plugin.MessageBridge.PluginConfig;
using CalendarConfig = HuaJiBot.NET.Plugin.Calendar.PluginConfig;
using GithubConfig = HuaJiBot.NET.Plugin.GitHubBridge.PluginConfig;
using RepairConfig = HuaJiBot.NET.Plugin.RepairTeam.PluginConfig;
using SummaryConfig = HuaJiBot.NET.Plugin.DailySummary.Config.PluginConfig;

namespace HuaJiBot.NET.Plugin.PushPanel;

internal static class PushRules
{
    public const string Github = "GitHubBridge";
    public const string Calendar = "Calendar";
    public const string Repair = "RepairTeam";
    public const string Bridge = "MessageBridge";
    public const string Summary = "每日聊天总结";

    internal static readonly JsonSerializerSettings Json = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Include,
    };

    public static PushRulesDocument Read(ConfigWrapper config) =>
        new()
        {
            Github = config.TryGetLive(Github, out var github) ? ReadGithub((GithubConfig)github) : null,
            Calendar = config.TryGetLive(Calendar, out var calendar)
                ? ReadCalendar((CalendarConfig)calendar)
                : null,
            Repair = config.TryGetLive(Repair, out var repair) ? ReadRepair((RepairConfig)repair) : null,
            Bridge = config.TryGetLive(Bridge, out var bridge)
                ? ReadBridge((BridgeConfig)bridge)
                : null,
            Summary = config.TryGetLive(Summary, out var summary)
                ? ReadSummary((SummaryConfig)summary)
                : null,
        };

    /// <returns>null when the change was saved.</returns>
    public static string? Apply(ConfigWrapper config, PushRulesDocument document)
    {
        var error = Validate(document);
        if (error is not null)
            return error;
        return config.Change(() =>
        {
            GithubConfig? github = null;
            CalendarConfig? calendar = null;
            RepairConfig? repair = null;
            BridgeConfig? bridge = null;
            SummaryConfig? summary = null;
            if (document.Github is not null && !TryLive(config, Github, out github))
                return "GitHubBridge 未加载";
            if (document.Calendar is not null && !TryLive(config, Calendar, out calendar))
                return "Calendar 未加载";
            if (document.Repair is not null && !TryLive(config, Repair, out repair))
                return "RepairTeam 未加载";
            if (document.Bridge is { } requestedBridge)
            {
                if (!TryLive(config, Bridge, out bridge) || bridge is null)
                    return "MessageBridge 未加载";
                var bridgeError = CheckBridge(bridge, requestedBridge);
                if (bridgeError is not null)
                    return bridgeError;
            }
            if (document.Summary is not null && !TryLive(config, Summary, out summary))
                return "每日聊天总结 未加载";

            if (document.Github is { } githubRules && github is not null)
                WriteGithub(github, githubRules);
            if (document.Calendar is { } calendarRules && calendar is not null)
                WriteCalendar(calendar, calendarRules);
            if (document.Repair is { } repairRules && repair is not null)
                WriteRepair(repair, repairRules);
            if (document.Bridge is { } bridgeRules && bridge is not null)
                WriteBridge(bridge, bridgeRules);
            if (document.Summary is { } summaryRules && summary is not null)
                WriteSummary(summary, summaryRules);
            return null;
        });
    }

    internal static string? Validate(PushRulesDocument document)
    {
        if (document.Github is { } github)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (name, targets) in github.Groups)
            {
                if (!IsGroupName(name))
                    return $"GitHub 分组名无效：{name}";
                if (!names.Add(name))
                    return $"GitHub 分组重复：{name}";
                var targetError = CheckTargets(targets, $"GitHub 分组 {name}");
                if (targetError is not null)
                    return targetError;
            }
            var repos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (repo, group) in github.Routes)
            {
                if (!IsRepo(repo))
                    return $"仓库名无效：{repo}";
                if (!repos.Add(repo))
                    return $"仓库重复：{repo}";
                if (!names.Contains(group))
                    return $"仓库 {repo} 指向不存在的分组 {group}";
            }
        }
        if (document.Calendar is { } calendar)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in calendar.Groups)
            {
                if (!IsTarget(group.GroupId))
                    return $"日程群号无效：{group.GroupId}";
                if (!ids.Add(group.GroupId))
                    return $"日程群号重复：{group.GroupId}";
                if (!Enum.TryParse<CalendarConfig.ReminderFilterConfig.FilterMode>(group.Mode, out _))
                    return $"日程过滤模式无效：{group.Mode}";
                if (group.Keywords.Any(string.IsNullOrWhiteSpace))
                    return "日程关键词不能为空";
            }
            if (calendar.WeeklyHour is < 0 or > 23 || calendar.DailyHour is < 0 or > 23)
                return "日程提醒小时必须在 0–23";
            if (calendar.WeeklyDay is < 0 or > 6)
                return "每周汇总的星期必须在 0–6";
        }
        if (document.Repair is { } repair)
        {
            if (repair.PushRawGroup.Length > 0 && !IsTarget(repair.PushRawGroup))
                return $"维修原始推送群号无效：{repair.PushRawGroup}";
            var info = CheckTargets(repair.PushInfoGroups, "维修格式化推送");
            if (info is not null)
                return info;
            var remind = CheckTargets(repair.RemindGroups, "维修每日摘要");
            if (remind is not null)
                return remind;
            if (repair.RemindHour is < 0 or > 23)
                return "维修摘要小时必须在 0–23";
            if (repair.OpenDays < 0 || repair.AcceptedDays < 0 || repair.CommittedDays < 0)
                return "维修卡住天数不能为负";
            if (
                !string.IsNullOrWhiteSpace(repair.RemindSince)
                && !DateTimeOffset.TryParse(
                    repair.RemindSince,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _
                )
            )
                return "RemindSince 不是时间";
        }
        if (document.Bridge is { } bridge)
        {
            foreach (var client in bridge.Clients)
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var group in client.Groups)
                {
                    if (!IsTarget(group.GroupId))
                        return $"消息桥群号无效：{group.GroupId}";
                    if (!ids.Add(group.GroupId))
                        return $"消息桥群号重复：{group.GroupId}";
                    foreach (var ev in group.DisabledEvents)
                    {
                        if (!Enum.TryParse<BridgeConfig.GroupConfig.ClientEventType>(ev, out _))
                            return $"消息桥事件无效：{ev}";
                    }
                }
            }
        }
        if (document.Summary is { } summary)
        {
            var groups = CheckTargets(summary.GroupIds, "每日总结");
            if (groups is not null)
                return groups;
            if (summary.Hour is < 0 or > 23 || summary.Minute is < 0 or > 59)
                return "每日总结时刻无效";
            if (summary.MinMessageCount < 0)
                return "最少消息数不能为负";
        }
        return null;
    }

    private static GithubRules ReadGithub(GithubConfig config) =>
        new()
        {
            Groups = config.BroadcastGroup.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToArray(),
                StringComparer.Ordinal
            ),
            Routes = config.BroadcastMap.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal
            ),
        };

    private static void WriteGithub(GithubConfig config, GithubRules rules)
    {
        config.BroadcastGroup.Clear();
        foreach (var (name, targets) in rules.Groups)
            config.BroadcastGroup[name] = targets.ToArray();
        config.BroadcastMap.Clear();
        foreach (var (repo, group) in rules.Routes)
            config.BroadcastMap[repo] = group;
    }

    private static CalendarRules ReadCalendar(CalendarConfig config) =>
        new()
        {
            Groups = config
                .ReminderGroups.Select(group => new CalendarGroupRule
                {
                    GroupId = group.GroupId,
                    Mode = group.Mode.ToString(),
                    Keywords = group.Keywords.ToArray(),
                })
                .ToArray(),
            WeeklyEnabled = config.ClubAffairsReminder.EnableWeeklySummary,
            DailyEnabled = config.ClubAffairsReminder.EnableDailyReminder,
            WeeklyHour = config.ClubAffairsReminder.WeeklySummaryHour,
            DailyHour = config.ClubAffairsReminder.DailyReminderHour,
            WeeklyDay = (int)config.ClubAffairsReminder.WeeklySummaryDayOfWeek,
        };

    private static void WriteCalendar(CalendarConfig config, CalendarRules rules)
    {
        config.ReminderGroups = rules
            .Groups.Select(group => new CalendarConfig.ReminderFilterConfig
            {
                GroupId = group.GroupId.Trim(),
                Mode = Enum.Parse<CalendarConfig.ReminderFilterConfig.FilterMode>(group.Mode),
                Keywords = group.Keywords.Select(keyword => keyword.Trim()).ToArray(),
            })
            .ToArray();
        config.ClubAffairsReminder.EnableWeeklySummary = rules.WeeklyEnabled;
        config.ClubAffairsReminder.EnableDailyReminder = rules.DailyEnabled;
        config.ClubAffairsReminder.WeeklySummaryHour = rules.WeeklyHour;
        config.ClubAffairsReminder.DailyReminderHour = rules.DailyHour;
        config.ClubAffairsReminder.WeeklySummaryDayOfWeek = (DayOfWeek)rules.WeeklyDay;
    }

    private static RepairRules ReadRepair(RepairConfig config) =>
        new()
        {
            PushRawGroup = config.PushRawGroup ?? "",
            PushInfoGroups = config.PushInfoGroup.ToArray(),
            RemindGroups = config.RemindGroups.ToArray(),
            RemindHour = config.RemindHour,
            RemindSince = config.RemindSince?.ToString("O"),
            OpenDays = config.OpenDays,
            AcceptedDays = config.AcceptedDays,
            CommittedDays = config.CommittedDays,
        };

    private static void WriteRepair(RepairConfig config, RepairRules rules)
    {
        config.PushRawGroup = rules.PushRawGroup.Trim();
        config.PushInfoGroup = rules.PushInfoGroups.Select(id => id.Trim()).ToArray();
        config.RemindGroups = rules.RemindGroups.Select(id => id.Trim()).ToArray();
        config.RemindHour = rules.RemindHour;
        config.RemindSince = string.IsNullOrWhiteSpace(rules.RemindSince)
            ? null
            : DateTimeOffset.Parse(rules.RemindSince, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        config.OpenDays = rules.OpenDays;
        config.AcceptedDays = rules.AcceptedDays;
        config.CommittedDays = rules.CommittedDays;
    }

    private static BridgeRules ReadBridge(BridgeConfig config) =>
        new()
        {
            Clients = config
                .Clients.Select(client => new BridgeClientRule
                {
                    Address = client.Address,
                    Groups = client
                        .Groups.Select(group => new BridgeGroupRule
                        {
                            GroupId = group.GroupId,
                            Enabled = group.Enabled,
                            ForwardToClient = group.ForwardToClient,
                            ForwardFromClient = group.ForwardFromClient,
                            DisabledEvents = group
                                .ForwardFromClientDisabledEvent.Select(item => item.ToString())
                                .Order(StringComparer.Ordinal)
                                .ToArray(),
                        })
                        .ToArray(),
                })
                .ToArray(),
        };

    private static bool TryLive<T>(ConfigWrapper config, string key, out T? live)
        where T : ConfigBase
    {
        if (config.TryGetLive(key, out var found) && found is T typed)
        {
            live = typed;
            return true;
        }
        live = null;
        return false;
    }

    private static string? CheckBridge(BridgeConfig config, BridgeRules rules)
    {
        if (rules.Clients.Length != config.Clients.Length)
            return "消息桥客户端数量不能在面板里增减";
        for (var i = 0; i < config.Clients.Length; i++)
        {
            if (!string.Equals(config.Clients[i].Address, rules.Clients[i].Address, StringComparison.Ordinal))
                return "消息桥地址不能在面板里修改";
        }
        return null;
    }

    private static void WriteBridge(BridgeConfig config, BridgeRules rules)
    {
        for (var i = 0; i < config.Clients.Length; i++)
        {
            config.Clients[i].Groups = rules
                .Clients[i]
                .Groups.Select(group => new BridgeConfig.GroupConfig
                {
                    GroupId = group.GroupId.Trim(),
                    Enabled = group.Enabled,
                    ForwardToClient = group.ForwardToClient,
                    ForwardFromClient = group.ForwardFromClient,
                    ForwardFromClientDisabledEvent = group
                        .DisabledEvents.Select(ev =>
                            Enum.Parse<BridgeConfig.GroupConfig.ClientEventType>(ev)
                        )
                        .ToHashSet(),
                })
                .ToArray();
        }
    }

    private static SummaryRules ReadSummary(SummaryConfig config) =>
        new()
        {
            GroupIds = config.GroupIds.ToArray(),
            Hour = config.SummaryHour,
            Minute = config.SummaryMinute,
            MinMessageCount = config.MinMessageCount,
        };

    private static void WriteSummary(SummaryConfig config, SummaryRules rules)
    {
        config.GroupIds.Clear();
        config.GroupIds.AddRange(rules.GroupIds.Select(id => id.Trim()));
        config.SummaryHour = rules.Hour;
        config.SummaryMinute = rules.Minute;
        config.MinMessageCount = rules.MinMessageCount;
    }

    private static string? CheckTargets(IEnumerable<string> targets, string label)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in targets)
        {
            if (!IsTarget(target))
                return $"{label}的目标无效：{target}";
            if (!seen.Add(target.Trim()))
                return $"{label}的目标重复：{target}";
        }
        return null;
    }

    private static bool IsGroupName(string name) =>
        name.Length is > 0 and <= 32 && name.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-');

    private static bool IsRepo(string repo)
    {
        var parts = repo.Split('/');
        return parts.Length == 2
            && parts[0].Length > 0
            && parts[1].Length > 0
            && parts.All(part => part.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '.' or '_' or '-'));
    }

    internal static bool IsTarget(string value)
    {
        var text = value.Trim();
        return text.Length is > 0 and <= 80
            && text.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or ':' || ch == '.');
    }
}

internal sealed class PushRulesDocument
{
    public GithubRules? Github { get; set; }
    public CalendarRules? Calendar { get; set; }
    public RepairRules? Repair { get; set; }
    public BridgeRules? Bridge { get; set; }
    public SummaryRules? Summary { get; set; }
}

internal sealed class GithubRules
{
    public Dictionary<string, string[]> Groups { get; set; } = new();
    public Dictionary<string, string> Routes { get; set; } = new();
}

internal sealed class CalendarRules
{
    public CalendarGroupRule[] Groups { get; set; } = [];
    public bool WeeklyEnabled { get; set; } = true;
    public bool DailyEnabled { get; set; } = true;
    public int WeeklyHour { get; set; } = 9;
    public int DailyHour { get; set; } = 9;
    public int WeeklyDay { get; set; } = 1;
}

internal sealed class CalendarGroupRule
{
    public string GroupId { get; set; } = "";
    public string Mode { get; set; } = "BlackList";
    public string[] Keywords { get; set; } = [];
}

internal sealed class RepairRules
{
    public string PushRawGroup { get; set; } = "";
    public string[] PushInfoGroups { get; set; } = [];
    public string[] RemindGroups { get; set; } = [];
    public int RemindHour { get; set; } = 12;
    public string? RemindSince { get; set; }
    public double OpenDays { get; set; } = 1;
    public double AcceptedDays { get; set; } = 3;
    public double CommittedDays { get; set; } = 2;
}

internal sealed class BridgeRules
{
    public BridgeClientRule[] Clients { get; set; } = [];
}

internal sealed class BridgeClientRule
{
    public string Address { get; set; } = "";
    public BridgeGroupRule[] Groups { get; set; } = [];
}

internal sealed class BridgeGroupRule
{
    public string GroupId { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool ForwardToClient { get; set; } = true;
    public bool ForwardFromClient { get; set; } = true;
    public string[] DisabledEvents { get; set; } = [];
}

internal sealed class SummaryRules
{
    public string[] GroupIds { get; set; } = [];
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int MinMessageCount { get; set; } = 10;
}
