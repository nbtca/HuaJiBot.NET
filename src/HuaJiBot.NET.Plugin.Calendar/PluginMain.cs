using System.Text;
using HuaJiBot.NET.Plugin.Calendar;
using Newtonsoft.Json;

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

    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        if (e.TextMessage.StartsWith("日程"))
        {
            var week = 1;
            var content = e.TextMessage[2..].Trim();
            if (!string.IsNullOrWhiteSpace(content))
            {
                if (!int.TryParse(content, out week))
                {
                    e.Feedback("参数错误");
                    return;
                }
            }
            var now = DateTime.Now;
            var end = now.AddDays(7 * week);
            StringBuilder sb = new();
            foreach (
                var (period, ev) in from x in ical.GetEvents(now, end)
                orderby x.period.StartTime ascending
                select x
            )
            {
                sb.AppendLine($"{period.StartTime:yyyy-MM-dd HH:mm} {ev.Summary}");
            }
            e.Feedback($"近{week}周的日程：\n{sb}");
            Service.LogDebug(JsonConvert.SerializeObject(e));
        }
        //Service.Log($"[{e.GroupName}] <{e.SenderMemberCard}> {e.TextMessage}");
    }

    protected override void Unload() { }
}
