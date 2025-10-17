using System.Text;
using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HuaJiBot.NET.Plugin.Calendar;

public class PluginConfig : ConfigBase
{
    public int MinRange = -128;
    public int MaxRange = 48;
    public ReminderFilterConfig[] ReminderGroups = [];

    public class ReminderFilterConfig
    {
        public string GroupId { get; set; } = "";
        public FilterMode Mode { get; set; } = FilterMode.WhiteList;
        public string[] Keywords { get; set; } = [];

        [JsonConverter(typeof(StringEnumConverter))]
        public enum FilterMode
        {
            WhiteList,
            BlackList,
            Default,
        }
    }
}

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    public PluginConfig Config { get; } = new();

    public PluginMain()
    {
        _sync = new(() => new(Service));
    }

    private readonly Lazy<RemoteSync> _sync;
    private RemoteSync Sync => _sync.Value;
    private Ical.Net.Calendar? Calendar => _sync.Value.Calendar;
    private ReminderTask? _reminderTask;
    private ClubAffairsReminder? _clubAffairsReminder;

    protected override void Initialize()
    {
        Service.Log("[日程] 启动成功！");
        _ = Sync.UpdateCalendarAsync();
        _reminderTask = new(
            Service,
            Config,
            () =>
            {
                _ = Sync.UpdateCalendarAsync();
                return Calendar;
            }
        );
        _reminderTask.Start();
        
        // 启动社团事务临期提醒功能
        _clubAffairsReminder = new(
            Service,
            Config,
            () =>
            {
                _ = Sync.UpdateCalendarAsync();
                return Calendar;
            }
        );
        _clubAffairsReminder.Start();
        Service.Log("[日程] 社团事务临期提醒功能已启动");
    }

    private readonly Dictionary<string, DateTimeOffset> _cache = new();

    [Command("最近日程", "查看最近一次日程详细信息")]
    // ReSharper disable once UnusedMember.Global
    public async Task CalendarCommandAsync(GroupMessageEventArgs e)
    {
        await Sync.UpdateCalendarAsync();
        const int coldDown = 10_000;
        var now = Utils.NetworkTime.Now;
        //Service.LogDebug(now.ToString("F"));
        if (_cache.TryGetValue(e.SenderId, out var lastTime))
        {
            var diff = (now - lastTime).TotalMilliseconds;
            if (diff < coldDown)
            {
                await e.Reply($"我知道你很急，但是你先别急，{(coldDown - diff) / 1000:F0}秒后再逝");
                return;
            }
        }
        _cache[e.SenderId] = now;
        _ = Task.Delay(coldDown).ContinueWith(_ => _cache.Remove(e.SenderId));
        if (Calendar is null)
        {
            await e.Reply("日历获取失败");
            return;
        }
        var all = Calendar.GetEvents(now, now.AddDays(14)).ToArray();
        if (all.Length == 0)
        {
            await e.Reply("没有日程");
            return;
        }
        var output = all.First().BuildTextOutput(now);
        await e.Reply(output);
    }

    [Command("日程", "查看近期日程")]
    // ReSharper disable once UnusedMember.Global
    public async Task CalendarCommandAsync(
        [CommandArgumentString("时间")] string? content,
        GroupMessageEventArgs e
    )
    {
        await Sync.UpdateCalendarAsync();
        const int coldDown = 10_000; //冷却时间
        var now = Utils.NetworkTime.Now; //当前时间
        //Service.LogDebug(now.ToString("F"));
        if (_cache.TryGetValue(e.SenderId, out var lastTime)) //如果缓存中有上次发送的时间
        {
            var diff = (now - lastTime).TotalMilliseconds; //计算时间差
            if (diff < coldDown) //如果小于冷却时间
            {
                await e.Reply($"我知道你很急，但是你先别急，{(coldDown - diff) / 1000:F0}秒后再逝");
                return;
            }
        }
        _cache[e.SenderId] = now; //更新缓存
        _ = Task.Delay(coldDown).ContinueWith(_ => _cache.Remove(e.SenderId)); //冷却时间后移除缓存
        var week = 1;
        if (!string.IsNullOrWhiteSpace(content)) //参数不为空
        {
            if (!int.TryParse(content, out week)) //尝试转换为数字表示周数
            {
                await e.Reply("参数错误");
                return;
            }
        }

        if (week < Config.MinRange || week > Config.MaxRange) //进行一个输入范围合法性检查
        {
            await e.Reply($"超出范围 [{Config.MinRange},{Config.MaxRange}] ");
            return;
        }
        DateTimeOffset start,
            end;
        if (week > 0) //正数表示未来
        {
            start = now; //开始于当前时间
            end = start.AddDays(7 * week); //当前时间加上周数
        }
        else
        {
            end = now; //结束时间
            start = end.AddDays(7 * week); //week是负的，所以开始时间等于现在减去..
        }
        if (Calendar is null)
        {
            await e.Reply("日历获取失败");
            return;
        }
        var output = Calendar.GetEvents(start, end).BuildTextOutput(now);
        await e.Reply($"近{week}周的日程：\n{output}");
        //Service.LogDebug(JsonConvert.SerializeObject(e));
    }

    public override IEnumerable<AgentFunctionInfo>? ExportFunctions
    {
        get
        {
            yield return new AgentFunctionInfo(
                () =>
                {
                    var now = Utils.NetworkTime.Now; //当前时间
                    if (Calendar is null)
                        return "日历获取失败";
                    var sb = new StringBuilder($"当前时间：{now}");
                    {
                        var start = now; //开始于当前时间
                        var end = start.AddDays(7); //当前时间加上周数
                        var output = Calendar.GetEvents(start, end).BuildTextOutput(now);
                        sb.AppendLine($"近一周的日程：\n{output}");
                    }
                    {
                        var start = now.AddDays(7); //开始于当前时间
                        var end = start.AddDays(30); //当前时间加上周数
                        var output = Calendar.GetEvents(start, end).BuildTextOutput(now);
                        sb.AppendLine($"一周后一个月内的日程：\n{output}");
                    }
                    return sb.ToString();
                },
                "GetCalendar",
                "获取最近一个月的所有日程"
            );
            yield return new AgentFunctionInfo(
                () =>
                {
                    if (Calendar is null)
                        return "日历获取失败";
                    var events = Calendar.GetEvents(
                        Utils.NetworkTime.Now,
                        Utils.NetworkTime.Now.AddDays(30)
                    );
                    if (events.FirstOrDefault() is { e: not null, period: not null } e)
                        return e.BuildTextOutput(Utils.NetworkTime.Now);
                    return "没有找到最近30天内的日程";
                },
                "GetRecentlyOneCalendarEvent",
                "仅获取最近一条日程"
            );
        }
    }

    protected override void Unload() 
    {
        _clubAffairsReminder?.Dispose();
    }
}
