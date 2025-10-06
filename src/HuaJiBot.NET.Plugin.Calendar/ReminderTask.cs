using System.Runtime.CompilerServices;
using HuaJiBot.NET.Bot;
using Ical.Net.CalendarComponents;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.Calendar;

internal class ReminderTask : IDisposable
{
    public PluginConfig Config { get; }
    public BotService Service { get; }
    private readonly Func<Ical.Net.Calendar?> _getCalendar;
    private Ical.Net.Calendar? Calendar => _getCalendar();
    private readonly Timer _timer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private int CheckDurationInMinutes => Config.CheckIntervalMinutes;
    private int RemindBeforeStartMinutes => Config.ReminderBeforeStartMinutes;
    private int RemindBeforeEndMinutes => Config.ReminderBeforeEndMinutes;

    public ReminderTask(
        BotService service,
        PluginConfig config,
        Func<Ical.Net.Calendar?> getCalendar
    )
    {
        Service = service;
        Config = config;
        _getCalendar = getCalendar;
        _timer = new(TimeSpan.FromMinutes(CheckDurationInMinutes)); //使用可配置的检查间隔
        _timer.Elapsed += (_, _) => InvokeCheck();
        Task.Delay(10_000, _cancellationTokenSource.Token)
            .ContinueWith(_ =>
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    InvokeCheck();
            }, TaskContinuationOptions.OnlyOnRanToCompletion); //10秒后检查第一次
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
    }

    private DateTimeOffset _scheduledTimeEnd = Utils.NetworkTime.Now; //截止到该时间点的日程已经在Task队列中列入计划了
    private readonly Dictionary<string, DateTimeOffset> _scheduledEvents = new(); //已经计划的事件，防止重复提醒，值为提醒时间

    private void ForEachMatchedGroup(CalendarEvent e, Action<Action<string>> callback)
    {
        foreach (var group in Config.ReminderGroups)
        {
            var list = group.Keywords;
            if (
                group.Mode switch
                {
                    // 默认发送
                    PluginConfig.ReminderFilterConfig.FilterMode.Default => true,
                    // 检查白名单
                    PluginConfig.ReminderFilterConfig.FilterMode.WhiteList => list.Any(x =>
                        (e.Summary?.Contains(x) ?? false)
                        || (e.Description?.Contains(x) ?? false)
                        || (e.Location ?? "").Contains(x)
                    ),
                    // 检查黑名单
                    PluginConfig.ReminderFilterConfig.FilterMode.BlackList => !list.Any(x =>
                        (e.Summary?.Contains(x) ?? false)
                        || (e.Description?.Contains(x) ?? false)
                        || (e.Location ?? "").Contains(x)
                    ),
                    _ => false,
                }
            )
            {
                callback(str => Service.SendGroupMessageAsync(null, group.GroupId, str));
            }
        }
    }

    //public string formatTimeRange(DateTime start, DateTime end)
    //{
    //    var s = new StringBuilder();
    //    var now = Utils.NetworkTime.Now; //现在
    //    //same day
    //    if (start.Date == end.Date)
    //    {
    //        s.Append(start.ToString("yyyy-MM-dd HH:mm"));
    //        s.Append(" - ");
    //        s.Append(end.ToString("HH:mm"));
    //    }
    //    else
    //    {
    //        s.Append(start.ToString("yyyy-MM-dd HH:mm"));
    //        s.Append(" - ");
    //        s.Append(end.ToString("yyyy-MM-dd HH:mm"));
    //    }
    //}

    private static string GetEventKey(CalendarEvent e, DateTimeOffset remindTime, string type)
    {
        // Create unique key combining event details to prevent collisions
        // Include event UID if available for better uniqueness
        var startTime = e.Start?.ToLocalNetworkTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "unknown";
        var endTime = e.End?.ToLocalNetworkTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "none";
        var summary = e.Summary?.Replace("|", "").Replace(":", "") ?? "nosummary";
        var location = e.Location?.Replace("|", "").Replace(":", "") ?? "";
        var uid = e.Uid ?? "";
        
        // Use a more robust key format that's less prone to collisions
        return $"{type}:{uid}:{summary}:{startTime}:{endTime}:{location}:{remindTime:yyyy-MM-dd-HH-mm-ss}";
    }

    private void CleanupOldScheduledEvents(DateTimeOffset now)
    {
        // 清理已经过时的计划事件记录，防止内存无限增长
        // 只移除提醒时间已经过去超过1小时的事件
        var cutoffTime = now.AddHours(-1);
        var keysToRemove = _scheduledEvents
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _scheduledEvents.Remove(key);
        }
        
        if (keysToRemove.Count > 0)
        {
            Service.LogDebug($"清理了 {keysToRemove.Count} 个过时的事件记录");
        }
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
            Service.LogDebug($"Invoke Check - 检查间隔: {CheckDurationInMinutes}分钟");
            var now = Utils.NetworkTime.Now; //现在
            var nextEnd = now.AddMinutes(CheckDurationInMinutes); //下次检查的结束时间（避免检查过的时间被重复添加进队列）
            var start = _scheduledTimeEnd; //从上次结束的时间点开始检查
            var end = nextEnd; //到下次结束的时间点结束检查
            _scheduledTimeEnd = nextEnd; //更新时间节点
            
            // 清理已经过时的计划事件记录，防止内存增长
            CleanupOldScheduledEvents(now);
            
            #region 开始提醒
            {
                //RemindBeforeStartMinutes 事件发生前 提前 _ 分钟提醒
                var remindStart = start.AddMinutes(RemindBeforeStartMinutes); //计算提醒开始时间
                var remindEnd = end.AddMinutes(RemindBeforeStartMinutes); //计算提醒结束时间
                foreach (
                    var (eventStartTime, e) in from x in Calendar.GetEvents(remindStart, remindEnd) //获取所有有交集的日程
                    let eventStartTime = x.period.StartTime
                    where eventStartTime >= remindStart && eventStartTime <= remindEnd //筛选开始时间在提醒时间段内的日程
                    select (eventStartTime, x.e)
                )
                {
                    var remindTime = eventStartTime.AddMinutes(-RemindBeforeStartMinutes); //计算提醒时间(RemindBeforeStartMinutes 分钟前提醒)
                    var timeRemained = remindTime - now; //计算距离提醒时间还有多久
                    if (timeRemained < TimeSpan.Zero) //如果已经开始了
                        continue; //跳过
                    
                    var eventKey = GetEventKey(e, remindTime, "start");
                    if (_scheduledEvents.ContainsKey(eventKey)) //如果已经计划过了
                        continue; //跳过
                    
                    _scheduledEvents[eventKey] = remindTime; //添加到已计划列表，记录提醒时间
                    ScheduleStartReminder(
                        timeRemained,
                        e,
                        eventKey,
                        ev =>
                        {
                            Service.Log(
                                $"[日程] {RemindBeforeStartMinutes} 分钟后开始日程：{ev.Summary}({ev.Start.ToLocalNetworkTime()})"
                            );
                            ForEachMatchedGroup(
                                ev,
                                send =>
                                    send(
                                        $"""
                                        日程提醒({ev.Start.ToLocalNetworkTime()})：
                                        {ev.Summary} {ev.Location}
                                        {ev.Description}
                                        将于 {RemindBeforeStartMinutes} 分钟后开始
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
                    let eventEndTime = x.period.EndTime
                    where eventEndTime >= remindStart && eventEndTime <= remindEnd //筛选结束时间在提醒时间段内的日程
                    select (eventEndTime, x.e)
                )
                {
                    var remindTime = eventEndTime.AddMinutes(-RemindBeforeEndMinutes); //计算提醒时间(RemindBeforeEndMinutes 分钟前提醒)
                    var timeRemained = remindTime - now; //计算距离提醒时间还有多久
                    if (timeRemained < TimeSpan.Zero) //如果已经开始了
                        continue; //跳过
                    if (e.End is null) //跳过没有结束时间的事件
                        continue;

                    var eventKey = GetEventKey(e, remindTime, "end");
                    if (_scheduledEvents.ContainsKey(eventKey)) //如果已经计划过了
                        continue; //跳过
                    
                    _scheduledEvents[eventKey] = remindTime; //添加到已计划列表，记录提醒时间
                    ScheduleStartReminder(
                        timeRemained,
                        e,
                        eventKey,
                        ev =>
                        {
                            Service.Log(
                                $"[日程] {RemindBeforeEndMinutes} 分钟后开始日程：{ev.Summary}({ev.Start?.ToLocalNetworkTime()})"
                            );
                            ForEachMatchedGroup(
                                ev,
                                send =>
                                    send(
                                        $"""
                                        日程提醒({ev.End?.ToLocalNetworkTime()})：
                                        {ev.Summary} {ev.Location}
                                        预计于 {RemindBeforeEndMinutes} 分钟后结束
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
        string eventKey,
        Action<CalendarEvent> start
    )
    {
        Task.Delay(waiting, _cancellationTokenSource.Token)
            .ContinueWith(_ => SendReminder(e, eventKey, start), 
                _cancellationTokenSource.Token,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        Service.Log(
            $"[日程] 计划发送提醒：{e.Summary}({e.Start?.ToLocalNetworkTime()}) ({waiting.TotalMinutes:F1}分钟后发送)"
        );
    }

    private void SendReminder(CalendarEvent e, string eventKey, Action<CalendarEvent> start)
    {
        try
        {
            Service.Log($"[日程] 发送提醒：{e.Summary}({e.Start?.ToLocalNetworkTime()})");
            start(e);
            
            // 提醒发送成功后，立即从计划列表中移除，防止内存泄漏
            _scheduledEvents.Remove(eventKey);
        }
        catch (Exception ex)
        {
            Service.LogError($"发送提醒失败：{e.Summary}", ex);
            // 即使发送失败也要清理，避免重试风暴
            _scheduledEvents.Remove(eventKey);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _timer.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
