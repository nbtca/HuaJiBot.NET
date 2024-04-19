using System.Net;
using System.Net.Sockets;

namespace HuaJiBot.NET.Utils;

public class NetworkTime
{
    private static TimeSpan _diff;

    public static async Task UpdateDiffAsync()
    {
        using var client = new HttpClient();
        var result = await client.GetAsync("https://baidu.com");
        if (result.Headers.TryGetValues("Date", out var times))
        {
            var time = DateTime.Parse(times.First());
            var localTime = DateTime.Now;
            _diff = time - localTime;
        }
    }

    public static DateTime Now => DateTime.Now + _diff;
    //public static async Task<DateTime> GetNetworkTime(string ntpServer = "time.windows.com")
    //{
    //    var cts = new CancellationTokenSource(3000);
    //    var ctk = cts.Token;
    //    var ntpData = new byte[48];
    //    ntpData[0] = 0x1B;
    //    var addresses = Dns.GetHostEntry(ntpServer).AddressList;
    //    var ipEndPoint = new IPEndPoint(addresses[0], 123);
    //    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    //    await socket.ConnectAsync(ipEndPoint, ctk);
    //    await socket.SendAsync(ntpData, ctk);
    //    await socket.ReceiveAsync(ntpData, ctk);
    //    socket.Close();
    //    var intPart = BitConverter.ToUInt32(ntpData, 43);
    //    var fractPart = BitConverter.ToUInt32(ntpData, 47);
    //    var milliseconds = (intPart * 1000) + fractPart * 1000 / 0x100000000L;
    //    var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(
    //        milliseconds
    //    );
    //    return networkDateTime.ToLocalTime();
    //}
}
