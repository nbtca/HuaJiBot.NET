using System.Runtime.CompilerServices;
using HuaJiBot.NET.Bot;
using Ical.Net.CalendarComponents;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.Calendar;

internal class ReminderTask : IDisposable
{
    public PluginConfig Config { get; }
    public BotServiceBase Service { get; }
    private readonly Func<Ical.Net.Calendar> _getCalendar;
    private Ical.Net.Calendar Calendar => _getCalendar();
    private readonly Timer _timer;
    private const int CheckDurationInMinutes = 15;
    private const int RemindBeforeStartMinutes = 15;
    private const int RemindBeforeEndMinutes = 5;

    public ReminderTask(
        BotServiceBase service,
        PluginConfig config,
        Func<Ical.Net.Calendar> getCalendar
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
                InvokeCheck();
            }); //10秒后检查第一次
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
    }

    private DateTime _scheduledTimeEnd = Utils.NetworkTime.Now; //截止到该时间点的日程已经在Task队列中列入计划了

    private void ForEachMatchedGroup(CalendarEvent e, Action<Action<string>> callback)
    {
        foreach (var groupId in Config.ReminderGroupIds)
        {
            callback(str => Service.SendGroupMessage(null, groupId, str));
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)] //防止多线程同时更新时间节点
    private void InvokeCheck()
    {
        try
        {
            Service.LogDebug("Invoke Check");
            var now = Utils.NetworkTime.Now; //现在
            var nextEnd = now.AddMinutes(CheckDurationInMinutes); //下次检查的结束时间
            var start = _scheduledTimeEnd; //从上次结束的时间点开始检查
            var end = nextEnd; //到下次结束的时间点结束检查
            _scheduledTimeEnd = nextEnd; //更新时间节点
            #region 开始提醒
            {
                //RemindBeforeStartMinutes 事件发生前 提前 5 分钟提醒
                var remindStart = start.AddMinutes(RemindBeforeStartMinutes); //计算提醒开始时间
                var remindEnd = end.AddMinutes(RemindBeforeStartMinutes); //计算提醒结束时间
                foreach (
                    var (eventStartTime, e) in from x in Calendar.GetEvents(remindStart, remindEnd) //获取所有有交集的日程
                    let eventStartTime = x.period.StartTime.AsSystemLocal
                    where eventStartTime >= remindStart && eventStartTime <= remindEnd //筛选开始时间在提醒时间段内的日程
                    select (eventStartTime, x.e)
                )
                {
                    var remindTime = eventStartTime.AddMinutes(-RemindBeforeStartMinutes); //计算提醒时间(RemindBeforeStartMinutes 分钟前提醒)
                    var timeRemained = remindTime - now; //计算距离提醒时间还有多久
                    if (timeRemained < TimeSpan.Zero) //如果已经开始了
                        continue; //跳过
                    ScheduleStartReminder(
                        timeRemained,
                        e,
                        ev =>
                        {
                            Service.Log(
                                $"[日程] {RemindBeforeStartMinutes} 分钟后开始日程：{ev.Summary}({ev.Start.AsSystemLocal})"
                            );
                            ForEachMatchedGroup(
                                ev,
                                send =>
                                    send(
                                        $"""
                                      日程提醒({ev.Start.AsSystemLocal})：
                                      {ev.Summary} {ev.Location}
                                      {ev.Description}
                                      将于 {RemindBeforeEndMinutes} 分钟后开始
                                      """
                                    )
                            );
                        }
                    ); //计划发送提醒
                }
            }
            #endregion
            #region 结束提醒
            {
                var remindStart = start.AddMinutes(RemindBeforeEndMinutes); //计算提醒开始时间
                var remindEnd = end.AddMinutes(RemindBeforeEndMinutes); //计算提醒结束时间
                foreach (
                    var (eventEndTime, e) in from x in Calendar.GetEvents(remindStart, remindEnd)
                    let eventEndTime = x.period.EndTime.AsSystemLocal
                    where eventEndTime >= remindStart && eventEndTime <= remindEnd //筛选结束时间在提醒时间段内的日程
                    select (eventEndTime, x.e)
                )
                {
                    var remindTime = eventEndTime.AddMinutes(-RemindBeforeEndMinutes); //计算提醒时间(RemindBeforeEndMinutes 分钟前提醒)
                    var timeRemained = remindTime - now; //计算距离提醒时间还有多久
                    if (timeRemained < TimeSpan.Zero) //如果已经开始了
                        continue; //跳过
                    ScheduleStartReminder(
                        timeRemained,
                        e,
                        ev =>
                        {
                            Service.Log(
                                $"[日程] {RemindBeforeEndMinutes} 分钟后开始日程：{ev.Summary}({ev.Start.AsSystemLocal})"
                            );
                            ForEachMatchedGroup(
                                ev,
                                send =>
                                    send(
                                        $"""
                                      日程提醒({ev.Start.AsSystemLocal})：
                                      {ev.Summary} {ev.Location}
                                      {ev.Description}
                                      预计于{RemindBeforeEndMinutes} 分钟后结束
                                      """
                                    )
                            );
                        }
                    ); //计划发送提醒
                }
            }
            #endregion
        }
        catch (Exception ex)
        {
            Service.LogError("日程提醒任务出现异常", ex);
        }
    }

    private void ScheduleStartReminder(
        TimeSpan waiting,
        CalendarEvent e,
        Action<CalendarEvent> start
    )
    {
        Task.Delay(waiting).ContinueWith(_ => SendReminder(e, start));
        Service.Log(
            $"[日程] 计划发送提醒：{e.Summary}({e.Start.AsSystemLocal}) ({waiting.TotalMinutes:F1}分钟后发送)"
        );
    }

    private void SendReminder(CalendarEvent e, Action<CalendarEvent> start)
    {
        Service.Log($"[日程] 发送提醒：{e.Summary}({e.Start.AsSystemLocal})");
        start(e);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
