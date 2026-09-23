using System.Runtime.CompilerServices;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Interfaces;
using Ical.Net.CalendarComponents;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.Calendar;

internal class ReminderTask : IDisposable
{
    public PluginConfig Config { get; }
    public IPluginService Service { get; }
    private readonly Func<Ical.Net.Calendar?> _getCalendar;
    private Ical.Net.Calendar? Calendar => _getCalendar();
    private readonly Timer _timer;
    private const int CheckDurationInMinutes = 15;
    private const int RemindBeforeStartMinutes = 60;
    private const int RemindBeforeEndMinutes = 5;

    public ReminderTask(
        IPluginService service,
        PluginConfig config,
        Func<Ical.Net.Calendar?> getCalendar
    )
    {
        Service = service;
        Config = config;
        _getCalendar = getCalendar;
        _timer = new(TimeSpan.FromMinutes(CheckDurationInMinutes)); //每15分钟检查一次
        _timer.Elapsed += (_, _) => InvokeCheck();
        Task.Delay(10_000)
            .ContinueWith(_ =>
            {
                InvokeCheck();
            }); //10秒后检查第一次
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
    }

    private DateTimeOffset _scheduledTimeEnd = Utils.NetworkTime.Now; //截止到该时间点的日程已经在Task队列中列入计划了

    private void SendToMatchedGroups(CalendarEvent e, Post post)
    {
        foreach (var group in Config.ReminderGroups.Where(g => g.Matches(e)))
            _ = Service.TrySendGroupMessageAsync(group.GroupId, post);
    }

    [MethodImpl(MethodImplOptions.Synchronized)] //防止多线程同时更新时间节点
    private void InvokeCheck()
    {
        try
        {
            if (Calendar is null)
            {
                Service.Log("日历为空，跳过检查。（日历未成功同步）");
                return;
            }
            Service.LogDebug("Invoke Check");
            var now = Utils.NetworkTime.Now; //现在
            var nextEnd = now.AddMinutes(CheckDurationInMinutes); //下次检查的结束时间（避免检查过的时间被重复添加进队列）
            var start = _scheduledTimeEnd; //从上次结束的时间点开始检查
            var end = nextEnd; //到下次结束的时间点结束检查
            _scheduledTimeEnd = nextEnd; //更新时间节点
            var remindStart = start.AddMinutes(RemindBeforeStartMinutes);
            var remindEnd = end.AddMinutes(RemindBeforeStartMinutes);
            foreach (var (period, e) in Calendar.GetEvents(remindStart, remindEnd))
            {
                if (e.IsAllDay || period.StartTime < remindStart || period.StartTime > remindEnd)
                    continue;
                ScheduleReminder(
                    period.StartTime.AddMinutes(-RemindBeforeStartMinutes) - now,
                    e,
                    StartPost(
                        period.StartTime,
                        e.End is null ? null : period.EndTime,
                        e,
                        RemindBeforeStartMinutes,
                        $"""
                        日程提醒（{period.StartTime:MM-dd HH:mm}）：
                        {e.Summary} {e.Location}
                        {e.Description}
                        将于 {RemindBeforeStartMinutes} 分钟后开始
                        """
                    )
                );
            }

            remindStart = start.AddMinutes(RemindBeforeEndMinutes);
            remindEnd = end.AddMinutes(RemindBeforeEndMinutes);
            foreach (var (period, e) in Calendar.GetEvents(remindStart, remindEnd))
            {
                if (e.IsAllDay || e.End is null || period.EndTime < remindStart || period.EndTime > remindEnd)
                    continue;
                ScheduleReminder(
                    period.EndTime.AddMinutes(-RemindBeforeEndMinutes) - now,
                    e,
                    EndPost(
                        e,
                        RemindBeforeEndMinutes,
                        $"""
                        日程提醒（{period.EndTime:MM-dd HH:mm}）：
                        {e.Summary} {e.Location}
                        预计于 {RemindBeforeEndMinutes} 分钟后结束
                        """
                    )
                );
            }
        }
        catch (Exception ex)
        {
            Service.LogError("日程提醒任务出现异常", ex);
        }
    }

    internal static Post StartPost(
        DateTimeOffset start,
        DateTimeOffset? end,
        CalendarEvent e,
        int minutes,
        string fallback
    )
    {
        var when = end is { } x ? $"🕘 {start:MM-dd HH:mm}–{x:HH:mm}" : $"🕘 {start:MM-dd HH:mm}";
        if (!string.IsNullOrWhiteSpace(e.Location))
            when += $" · 📍 {Post.Escape(e.Location)}";
        var markdown = $"⏰ **{Post.Escape(e.Summary ?? "")}** {minutes} 分钟后开始\n{when}";
        if (!string.IsNullOrWhiteSpace(e.Description))
            markdown +=
                "\n\n"
                + string.Join(
                    "\n",
                    from line in e.Description.ReplaceLineEndings("\n").Trim().Split('\n')
                    select "> " + Post.Escape(line)
                );
        return new(markdown, fallback) { Tags = ["日程"] };
    }

    internal static Post EndPost(CalendarEvent e, int minutes, string fallback) =>
        new($"🔚 **{Post.Escape(e.Summary ?? "")}** 预计 {minutes} 分钟后结束", fallback)
        {
            Tags = ["日程"],
            Silent = true,
        };

    private void ScheduleReminder(TimeSpan waiting, CalendarEvent e, Post post)
    {
        if (waiting < TimeSpan.Zero)
            return;
        Task.Delay(waiting)
            .ContinueWith(_ =>
            {
                Service.Log($"[日程] 发送提醒：{e.Summary}");
                SendToMatchedGroups(e, post);
            });
        Service.Log($"[日程] 计划发送提醒：{e.Summary} ({waiting.TotalMinutes:F1}分钟后发送)");
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
