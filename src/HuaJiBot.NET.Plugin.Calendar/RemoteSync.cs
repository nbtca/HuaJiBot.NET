using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Utils;

namespace HuaJiBot.NET.Plugin.Calendar;

internal class RemoteSync(
    BotService service,
    int updateDurationInMinutes = 15,
    string icalUrl = "https://ical.nbtca.space/"
)
{
    private DateTimeOffset _lastLoadTime = DateTimeOffset.MinValue;
    public Ical.Net.Calendar? Calendar { get; private set; }

    public async Task UpdateCalendarAsync()
    {
        var now = Utils.NetworkTime.Now;
        lock (this) //上锁，防止同时多次更新日历
        {
            if (now - _lastLoadTime < TimeSpan.FromMinutes(updateDurationInMinutes)) //如果距离上次加载小于指定时长
            {
                return; //直接返回
            }
            //否则重新加载
            _lastLoadTime = Utils.NetworkTime.Now;
        }
        try
        {
            HttpClient client = new();
            var resp = await client.GetAsync(icalUrl); //从Url获取
            resp.EnsureSuccessStatusCode();
            Calendar = Ical.Net.Calendar.Load(await resp.Content.ReadAsStringAsync());
            if (Calendar is null)
            {
                service.LogError(nameof(UpdateCalendarAsync), "日历加载失败");
                return;
            }
            service.Log(
                $"日历更新成功 当前时间 {NetworkTime.Now} 网络时间差分 {NetworkTime.Diff:g}"
            );
            //var testStart = DateTimeOffset.Parse("4/19/2024 8:25:27 PM +08:00");
            //var testEnd = testStart;
            var testStart = NetworkTime.Now;
            var testEnd = testStart + TimeSpan.FromDays(7);
            foreach (var (period, e) in Calendar.GetEvents(testStart, testEnd))
            {
                service.Log(period.StartTime + " --> " + period.EndTime + " : " + e.Summary);
            }
        }
        catch (Exception ex)
        {
            service.LogError(nameof(UpdateCalendarAsync), ex);
        }
    }
}
