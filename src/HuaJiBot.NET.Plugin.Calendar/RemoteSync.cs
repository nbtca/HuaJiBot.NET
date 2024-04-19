using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Plugin.Calendar;

internal class RemoteSync(
    BotServiceBase service,
    int updateDurationInMinutes = 15,
    string icalUrl = "https://ical.nbtca.space/"
)
{
    private DateTime _lastLoadTime = DateTime.MinValue;
    public Ical.Net.Calendar Calendar { get; private set; } = null!;

    public async Task UpdateCalendarAsync()
    {
        var now = Utils.NetworkTime.Now;
        lock (this) //锁定，防止同时多次更新日历
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
            service.Log("日历更新成功");
        }
        catch (Exception ex)
        {
            service.LogError(nameof(UpdateCalendarAsync), ex);
        }
    }
}
