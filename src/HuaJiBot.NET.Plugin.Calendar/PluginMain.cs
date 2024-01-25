using System.Text;
using Ical.Net.DataTypes;

namespace HuaJiBot.NET.Plugin.Calendar;

public class PluginConfig : ConfigBase
{
    public int MinRange = -128;
    public int MaxRange = 48;
    public string[] ReminderGroupIds = Array.Empty<string>();
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    public PluginConfig Config { get; } = new();

    public PluginMain()
    {
        _sync = new(() => new(Service));
    }

    private readonly Lazy<RemoteSync> _sync;
    private RemoteSync Sync => _sync.Value;
    private Ical.Net.Calendar Calendar => _sync.Value.Calendar;
    private ReminderTask? _reminderTask;

    protected override void Initialize()
    {
        //订阅群消息事件
        Service.Events.OnGroupMessageReceived += Events_OnGroupMessageReceived;
        Service.Log("[日程] 启动成功！");
        Sync.UpdateCalendar();
        _reminderTask = new(
            Service,
            Config,
            () =>
            {
                Sync.UpdateCalendar();
                return Calendar;
            }
        );
        _reminderTask.Start();
    }

    private readonly Dictionary<string, DateTime> _cache = new();

    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        Task.Run(async () =>
        {
            if (e.TextMessage.StartsWith("日程"))
            {
                await Sync.UpdateCalendar();
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
                var output = Calendar.GetEvents(start, end).BuildTextOutput(now);
                e.Feedback($"近{week}周的日程：\n{output}");
                //Service.LogDebug(JsonConvert.SerializeObject(e));
            }
        });
    }

    protected override void Unload() { }
}
