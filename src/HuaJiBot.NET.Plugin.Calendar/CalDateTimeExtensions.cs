using HuaJiBot.NET.Utils;
using Ical.Net.DataTypes;

namespace HuaJiBot.NET.Plugin.Calendar;

internal static class CalDateTimeExtensions
{
    public static DateTimeOffset ToLocalNetworkTime(this CalDateTime @this)
    {
        return new DateTimeOffset(@this.AsUtc, NetworkTime.LocalTimeZoneOffset);
    }
}
