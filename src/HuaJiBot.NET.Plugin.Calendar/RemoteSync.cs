using HuaJiBot.NET.Bot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Plugin.Calendar;

internal class RemoteSync(
    BotServiceBase service,
    int updateDurationInMinutes = 15,
    string icalUrl = "https://i.nbtca.space/panel/ical"
)
{
    private DateTime _lastLoadTime = DateTime.MinValue;
    public Ical.Net.Calendar Calendar { get; private set; } = null!;

    public Task UpdateCalendar()
    {
        lock (this) //锁定，防止同时多次更新日历
        {
            if (DateTime.Now - _lastLoadTime < TimeSpan.FromMinutes(updateDurationInMinutes)) //如果距离上次加载小于指定时长
            {
                return Task.CompletedTask; //直接返回
            }
            //否则重新加载
            _lastLoadTime = DateTime.Now;
        }
        return Task.Run(async () =>
        {
            try
            {
                HttpClient client = new();
                var resp = await client.GetAsync(icalUrl); //从Url获取
                resp.EnsureSuccessStatusCode();
                Calendar = Ical.Net.Calendar.Load(await resp.Content.ReadAsStringAsync());
                service.Log("日历更新成功");
                //var now = DateTime.Now;
                //var end = now.AddDays(14);
                //foreach (var (period, e) in Calendar.GetEvents(now, end))
                //{
                //    service.Log(period.StartTime);
                //    service.Log(e.Summary);
                //    service.Log(e.Description);
                //}
            }
            catch (Exception ex)
            {
                service.LogError(nameof(UpdateCalendar), ex);
            }
        });
    }
}
