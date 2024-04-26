using System.Net;
using System.Net.Sockets;

namespace HuaJiBot.NET.Utils;

public class NetworkTime
{
    private static TimeSpan _diff;
    public static TimeSpan Diff => _diff;
    public static TimeSpan LocalTimeZoneOffset = TimeSpan.FromHours(8);

    public static async Task UpdateDiffAsync()
    {
        using var client = new HttpClient();
        var result = await client.GetAsync("https://baidu.com");
        if (result.Headers.TryGetValues("Date", out var times))
        {
            var time = DateTimeOffset.Parse(times.First());
            var localTime = DateTimeOffset.Now;
            _diff = time - localTime;
        }
    }

    public static void TryUpdateTimeDiff()
    {
        UpdateDiffAsync()
            .ContinueWith(
                e =>
                {
                    if (e.IsFaulted)
                    {
                        Console.WriteLine(e.Exception);
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted
            );
    }

    public static DateTimeOffset Now =>
        (DateTimeOffset.Now + _diff).ToOffset(TimeSpan.FromHours(8));
}
