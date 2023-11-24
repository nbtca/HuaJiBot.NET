using System.Text;
using Ical.Net.DataTypes;

namespace HuaJiBot.NET.Plugin.Calendar;

public class PluginConfig : ConfigBase
{
    public int MinRange = -128;
    public int MaxRange = 48;
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    public PluginConfig Config { get; } = new();

    public PluginMain()
    {
        _sync = new(() => new(Service));
    }

    private readonly Lazy<RemoteSync> _sync;
    private RemoteSync sync => _sync.Value;
    private Ical.Net.Calendar ical => _sync.Value.Calendar;

    protected override Task Initialize()
    {
        //订阅群消息事件
        Service.Events.OnGroupMessageReceived += Events_OnGroupMessageReceived;
        Service.Log("[日程] 启动成功！");
        sync.LoadCalendar();
        return Task.CompletedTask;
    }

    private readonly Dictionary<string, DateTime> _cache = new();

    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        Task.Run(async () =>
        {
            if (e.TextMessage.StartsWith("日程"))
            {
                await sync.LoadCalendar();
                const int coldDown = 10_000; //冷却时间
                var now = DateTime.Now; //当前时间
                if (_cache.TryGetValue(e.SenderId, out var lastTime)) //如果缓存中有上次发送的时间
                {
                    var diff = (now - lastTime).TotalMilliseconds; //计算时间差
                    if (diff < coldDown) //如果小于冷却时间
                    {
                        e.Feedback($"我知道你很急，但是你先别急，{(coldDown - diff) / 1000:F0}秒后再逝");
                        return;
                    }
                }
                _cache[e.SenderId] = now; //更新缓存
                _ = Task.Delay(coldDown).ContinueWith(_ => _cache.Remove(e.SenderId)); //冷却时间后移除缓存
                var week = 1;
                var content = e.TextMessage[2..].Trim(); //去除前两个字符后的文本内容
                if (!string.IsNullOrWhiteSpace(content)) //参数不为空
                {
                    if (!int.TryParse(content, out week)) //尝试转换为数字表示周数
                    {
                        e.Feedback("参数错误");
                        return;
                    }
                }

                if (week < Config.MinRange || week > Config.MaxRange) //进行一个输入范围合法性检查
                {
                    e.Feedback($"超出范围 [{Config.MinRange},{Config.MaxRange}] ");
                    return;
                }
                DateTime start,
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
                StringBuilder sb = new();
                foreach (var (period, ev) in ical.GetEvents(start, end)) //遍历每一个事件
                {
                    string FormatTimeWeek(IDateTime date)
                    {
                        var week = date.DayOfWeek switch
                        {
                            DayOfWeek.Monday => "一",
                            DayOfWeek.Tuesday => "二",
                            DayOfWeek.Wednesday => "三",
                            DayOfWeek.Thursday => "四",
                            DayOfWeek.Friday => "五",
                            DayOfWeek.Saturday => "六",
                            DayOfWeek.Sunday => "日",
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        var dateOffset = date.AsDateTimeOffset;
                        if (dateOffset.Date == now.Date)
                            return $"{dateOffset:MM-dd} 今天 周{week} {dateOffset:HH:mm}";
                        if (dateOffset.Date == now.Date.AddDays(1))
                            return $"{dateOffset:MM-dd} 明天 周{week} {dateOffset:HH:mm}";
                        if (dateOffset.Date == now.Date.AddDays(2))
                            return $"{dateOffset:MM-dd} 后天 周{week} {dateOffset:HH:mm}";
                        if (dateOffset.Year == now.Year) //same year
                            return $"{dateOffset:MM-dd} 周{week} {dateOffset:HH:mm}";
                        return $"{dateOffset:yyyy-MM-dd} 周{week} {dateOffset:HH:mm}";
                    }
                    //判断是同一天
                    if (period.StartTime.Date != period.EndTime.Date)
                    {
                        sb.AppendLine(
                            $"{FormatTimeWeek(period.StartTime)} ~ {FormatTimeWeek(period.EndTime)}"
                        ); //输出
                    }
                    else
                    {
                        sb.AppendLine(
                            $"{FormatTimeWeek(period.StartTime)} ~ {period.EndTime.AsDateTimeOffset:HH:mm}"
                        ); //输出
                    }
                    if (!string.IsNullOrWhiteSpace(ev.Summary))
                        sb.AppendLine($"    概要：{ev.Summary}");
                    if (!string.IsNullOrWhiteSpace(ev.Location))
                        sb.AppendLine($"    地点：{ev.Location}");
                    if (!string.IsNullOrWhiteSpace(ev.Description))
                        sb.AppendLine($"    描述：{ev.Description}");
                }
                e.Feedback($"近{week}周的日程：\n{sb}");
                //Service.LogDebug(JsonConvert.SerializeObject(e));
            }
        });
    }

    protected override void Unload() { }
}
