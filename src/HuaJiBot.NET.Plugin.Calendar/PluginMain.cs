using System.Text;
using HuaJiBot.NET.Plugin.Calendar;

namespace HuaJiBot.NET.Plugin.RepairTeam;

public class PluginMain : PluginBase
{
    protected override Task Initialize()
    {
        //订阅群消息事件
        Service.Events.OnGroupMessageReceived += Events_OnGroupMessageReceived;
        Service.Log("[日程] 启动成功！");
        LoadCalendar();
        return Task.CompletedTask;
    }

    private const string icalUrl =
        "https://p203-caldav.icloud.com.cn/published/2/MTE2MDg5MTA3NTIxMTYwOJ62pmQYH-orN1EZPCTNLzb42OJtGwf4PeI0ojg6fXjzh83-l1lgCdpnbRdCvNPzgjuJI3hmuh3AUfSvecozMv4";

    private Ical.Net.Calendar ical;

    private void LoadCalendar()
    {
        Task.Run(async () =>
        {
            try
            {
                HttpClient client = new();
                var resp = await client.GetAsync(icalUrl); //从Url获取
                resp.EnsureSuccessStatusCode();
                ical = Ical.Net.Calendar.Load(await resp.Content.ReadAsStringAsync());
                var now = DateTime.Now;
                var end = now.AddDays(7);
                foreach (var (period, e) in ical.GetEvents(now, end))
                {
                    Service.Log(period.StartTime);
                    Service.Log(e.Summary);
                }
            }
            catch (Exception ex)
            {
                Service.LogError(nameof(LoadCalendar), ex);
            }
        });
    }

    private readonly Dictionary<string, DateTime> _cache = new();

    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        if (e.TextMessage.StartsWith("日程"))
        {
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
            Task.Delay(coldDown).ContinueWith(_ => _cache.Remove(e.SenderId)); //冷却时间后移除缓存
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
            const int minRange = -128;
            const int maxRange = 48;
            if (week is > maxRange or < minRange or 0) //进行一个输入范围合法性检查
            {
                e.Feedback($"超出范围 [{minRange},{maxRange}] ");
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
            foreach (
                var (period, ev) in from x in ical.GetEvents(start, end) //选择
                orderby x.period.StartTime ascending //按照开始时间排序
                select x //映射
            ) //循环
            {
                sb.AppendLine($"{period.StartTime:yyyy-MM-dd HH:mm} {ev.Summary}"); //输出
            }
            e.Feedback($"近{week}周的日程：\n{sb}");
            //Service.LogDebug(JsonConvert.SerializeObject(e));
        }
    }

    protected override void Unload() { }
}
