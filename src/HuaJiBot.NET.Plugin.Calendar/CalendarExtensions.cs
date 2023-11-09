using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace HuaJiBot.NET.Plugin.Calendar;

internal static class CalendarExtensions
{
    public static IEnumerable<(Period period, CalendarEvent e)> GetEvents(
        this Ical.Net.Calendar @this,
        DateTime start,
        DateTime end
    )
    {
        foreach (var occurrence in @this.GetOccurrences(start, end))
        {
            yield return occurrence.Source switch
            {
                CalendarEvent calendarEvent => (occurrence.Period, calendarEvent),
                _
                    => throw new ArgumentOutOfRangeException(
                        "not impl " + occurrence.Source.GetType()
                    )
            };
        }
    }
}
