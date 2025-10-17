using System.Text;
using HuaJiBot.NET.Bot;
using Ical.Net.CalendarComponents;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.Calendar;

/// <summary>
/// 社团事务临期提醒功能
/// 提供一周预告和一天提醒功能
/// </summary>
internal class ClubAffairsReminder : IDisposable
{
    public PluginConfig Config { get; }
    public BotService Service { get; }
    private readonly Func<Ical.Net.Calendar?> _getCalendar;
    private Ical.Net.Calendar? Calendar => _getCalendar();
    private readonly Timer _dailyCheckTimer;
    private const int CheckIntervalMinutes = 60; // 每小时检查一次
    private DateTimeOffset _lastWeeklySummaryDate = DateTimeOffset.MinValue;
    private DateTimeOffset _lastDailyReminderDate = DateTimeOffset.MinValue;

    public ClubAffairsReminder(
        BotService service,
        PluginConfig config,
        Func<Ical.Net.Calendar?> getCalendar
    )
    {
        Service = service;
        Config = config;
        _getCalendar = getCalendar;
        _dailyCheckTimer = new Timer(TimeSpan.FromMinutes(CheckIntervalMinutes));
        _dailyCheckTimer.Elapsed += (_, _) => CheckAndSendReminders();
        _dailyCheckTimer.AutoReset = true;

        // 初始延迟检查
        Task.Delay(30_000).ContinueWith(_ => CheckAndSendReminders());
    }

    public void Start()
    {
        _dailyCheckTimer.Start();
    }

    private void CheckAndSendReminders()
    {
        try
        {
            var now = Utils.NetworkTime.Now;

            // 检查是否应该发送每周汇总
            if (Config.ClubAffairsReminder.EnableWeeklySummary && ShouldSendWeeklySummary(now))
            {
                SendWeeklySummary(now);
                _lastWeeklySummaryDate = now.Date;
            }

            // 检查是否应该发送每日提醒
            if (Config.ClubAffairsReminder.EnableDailyReminder && ShouldSendDailyReminder(now))
            {
                SendDailyReminder(now);
                _lastDailyReminderDate = now.Date;
            }
        }
        catch (Exception ex)
        {
            Service.LogError("社团事务提醒任务出现异常", ex);
        }
    }

    private bool ShouldSendWeeklySummary(DateTimeOffset now)
    {
        // 根据配置的星期几发送
        if (now.DayOfWeek != Config.ClubAffairsReminder.WeeklySummaryDayOfWeek)
            return false;

        // 根据配置的小时发送
        if (now.Hour != Config.ClubAffairsReminder.WeeklySummaryHour)
            return false;

        // 检查今天是否已经发送过
        return now.Date != _lastWeeklySummaryDate.Date;
    }

    private bool ShouldSendDailyReminder(DateTimeOffset now)
    {
        // 根据配置的小时发送
        if (now.Hour != Config.ClubAffairsReminder.DailyReminderHour)
            return false;

        // 检查今天是否已经发送过
        return now.Date != _lastDailyReminderDate.Date;
    }

    private void SendWeeklySummary(DateTimeOffset now)
    {
        if (Calendar is null)
        {
            Service.Log("[社团事务] 日历为空，跳过每周汇总发送");
            return;
        }

        var weekStart = now;
        var weekEnd = now.AddDays(7);

        var upcomingEvents = Calendar
            .GetEvents(weekStart, weekEnd)
            .OrderBy(x => x.period.StartTime)
            .ToList();

        if (upcomingEvents.Count == 0)
        {
            Service.Log("[社团事务] 本周无即将到期的事务");
            return;
        }

        var message = BuildWeeklySummaryMessage(now, weekEnd, upcomingEvents);

        foreach (var group in Config.ReminderGroups)
        {
            if (group.Mode == PluginConfig.ReminderFilterConfig.FilterMode.Default ||
                ShouldSendToGroup(upcomingEvents, group))
            {
                Service.SendGroupMessageAsync(null, group.GroupId, message);
                Service.Log($"[社团事务] 已向群组 {group.GroupId} 发送每周汇总");
            }
        }
    }

    private void SendDailyReminder(DateTimeOffset now)
    {
        if (Calendar is null)
        {
            Service.Log("[社团事务] 日历为空，跳过每日提醒发送");
            return;
        }

        var tomorrow = now.Date.AddDays(1);
        var dayAfterTomorrow = tomorrow.AddDays(1);

        var tomorrowEvents = Calendar
            .GetEvents(tomorrow, dayAfterTomorrow)
            .OrderBy(x => x.period.StartTime)
            .ToList();

        if (tomorrowEvents.Count == 0)
        {
            Service.Log("[社团事务] 明天无即将到期的事务");
            return;
        }

        var message = BuildDailyReminderMessage(tomorrow, tomorrowEvents);

        foreach (var group in Config.ReminderGroups)
        {
            var groupEvents = FilterEventsForGroup(tomorrowEvents, group);
            if (groupEvents.Count > 0)
            {
                var groupMessage = BuildDailyReminderMessage(tomorrow, groupEvents);
                Service.SendGroupMessageAsync(null, group.GroupId, groupMessage);
                Service.Log($"[社团事务] 已向群组 {group.GroupId} 发送每日提醒");
            }
        }
    }

    private bool ShouldSendToGroup(
        List<(CalendarExtensions.Period period, CalendarEvent e)> events,
        PluginConfig.ReminderFilterConfig group
    )
    {
        if (group.Mode == PluginConfig.ReminderFilterConfig.FilterMode.Default)
            return true;

        return events.Any(evt =>
        {
            var e = evt.e;
            var list = group.Keywords;
            return group.Mode switch
            {
                PluginConfig.ReminderFilterConfig.FilterMode.WhiteList => list.Any(x =>
                    (e.Summary?.Contains(x) ?? false)
                    || (e.Description?.Contains(x) ?? false)
                    || (e.Location ?? "").Contains(x)
                ),
                PluginConfig.ReminderFilterConfig.FilterMode.BlackList => !list.Any(x =>
                    (e.Summary?.Contains(x) ?? false)
                    || (e.Description?.Contains(x) ?? false)
                    || (e.Location ?? "").Contains(x)
                ),
                _ => false,
            };
        });
    }

    private List<(CalendarExtensions.Period period, CalendarEvent e)> FilterEventsForGroup(
        List<(CalendarExtensions.Period period, CalendarEvent e)> events,
        PluginConfig.ReminderFilterConfig group
    )
    {
        if (group.Mode == PluginConfig.ReminderFilterConfig.FilterMode.Default)
            return events;

        return events
            .Where(evt =>
            {
                var e = evt.e;
                var list = group.Keywords;
                return group.Mode switch
                {
                    PluginConfig.ReminderFilterConfig.FilterMode.WhiteList => list.Any(x =>
                        (e.Summary?.Contains(x) ?? false)
                        || (e.Description?.Contains(x) ?? false)
                        || (e.Location ?? "").Contains(x)
                    ),
                    PluginConfig.ReminderFilterConfig.FilterMode.BlackList => !list.Any(x =>
                        (e.Summary?.Contains(x) ?? false)
                        || (e.Description?.Contains(x) ?? false)
                        || (e.Location ?? "").Contains(x)
                    ),
                    _ => true,
                };
            })
            .ToList();
    }

    private string BuildWeeklySummaryMessage(
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        List<(CalendarExtensions.Period period, CalendarEvent e)> events
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"📢 社团事务一周预告（{weekStart:MM月dd日}～{weekEnd:MM月dd日}）"
        );
        sb.AppendLine("以下事务将在一周内到期，请相关负责人提前准备：");
        sb.AppendLine();

        for (int i = 0; i < events.Count; i++)
        {
            var (period, e) = events[i];
            var emoji = GetNumberEmoji(i + 1);
            var dateStr = period.StartTime.ToString("MM月dd日");
            var responsible = ExtractResponsible(e);
            sb.AppendLine(
                $"{emoji} [{dateStr}] {e.Summary ?? "未命名事务"}{(string.IsNullOrEmpty(responsible) ? "" : $"（负责人：{responsible}）")}"
            );
        }

        sb.AppendLine();
        sb.AppendLine("✅ 请大家合理安排时间，确保事项按时完成。");
        sb.AppendLine("—— 社团事务提醒机器人 🤖");

        return sb.ToString();
    }

    private string BuildDailyReminderMessage(
        DateTimeOffset tomorrow,
        List<(CalendarExtensions.Period period, CalendarEvent e)> events
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("⏰ 临期提醒");
        sb.AppendLine($"明天（{tomorrow:MM月dd日}）截止的社团事务：");
        sb.AppendLine();

        foreach (var (period, e) in events)
        {
            var responsible = ExtractResponsible(e);
            sb.AppendLine(
                $"• {e.Summary ?? "未命名事务"}{(string.IsNullOrEmpty(responsible) ? "" : $"（负责人：{responsible}）")}"
            );
        }

        sb.AppendLine();
        sb.AppendLine("请务必在截止前完成相关工作！");
        sb.AppendLine("—— 社团事务提醒机器人 🤖");

        return sb.ToString();
    }

    private string ExtractResponsible(CalendarEvent e)
    {
        // 尝试从描述中提取负责人信息
        if (string.IsNullOrWhiteSpace(e.Description))
            return string.Empty;

        // 查找常见的负责人标识
        var patterns = new[] { "负责人：", "负责人:", "责任人：", "责任人:" };
        foreach (var pattern in patterns)
        {
            var index = e.Description.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var startIndex = index + pattern.Length;
                var endIndex = e.Description.IndexOfAny(
                    new[] { '\n', '\r', '）', ')', '，', ',' },
                    startIndex
                );
                if (endIndex > startIndex)
                {
                    return e.Description.Substring(startIndex, endIndex - startIndex).Trim();
                }
                else if (startIndex < e.Description.Length)
                {
                    var remaining = e.Description.Substring(startIndex).Trim();
                    // 取前20个字符作为负责人名字
                    return remaining.Length > 20 ? remaining.Substring(0, 20) : remaining;
                }
            }
        }

        return string.Empty;
    }

    private string GetNumberEmoji(int number)
    {
        return number switch
        {
            1 => "1️⃣",
            2 => "2️⃣",
            3 => "3️⃣",
            4 => "4️⃣",
            5 => "5️⃣",
            6 => "6️⃣",
            7 => "7️⃣",
            8 => "8️⃣",
            9 => "9️⃣",
            10 => "🔟",
            _ => $"{number}.",
        };
    }

    public void Dispose()
    {
        _dailyCheckTimer?.Dispose();
    }
}
