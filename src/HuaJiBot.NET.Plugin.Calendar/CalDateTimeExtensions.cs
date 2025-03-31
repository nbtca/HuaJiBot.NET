using HuaJiBot.NET.Utils;
using Ical.Net.DataTypes;

namespace HuaJiBot.NET.Plugin.Calendar;

internal static class CalDateTimeExtensions
{
    public static DateTimeOffset ToLocalNetworkTime(this CalDateTime @this)
    {
        var utcDateTime = @this.AsUtc;
        return new DateTimeOffset(utcDateTime.Ticks, TimeSpan.Zero).ToOffset(
            NetworkTime.LocalTimeZoneOffset
        );
    }
}
