using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace HuaJiBot.NET.Plugin.Calendar;

internal static class CalendarExtensions
{
    /// <summary>
    /// 扩展方法，
    /// 获取指定时间范围内所有的事件
    /// 并按照开始时间排序
    /// </summary>
    /// <param name="this">扩展方法this</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IEnumerable<(Period period, CalendarEvent e)> GetEvents(
        this Ical.Net.Calendar @this,
        DateTime start,
        DateTime end
    ) =>
       from occurrence in @this.GetOccurrences(start, end)
       select//选择
           occurrence.Source switch
           {
               CalendarEvent calendarEvent => (occurrence.Period, calendarEvent),
               _
                   => throw new ArgumentOutOfRangeException(
                       "not impl " + occurrence.Source.GetType()
                   )
           }
       into tuple
       orderby tuple.Period.StartTime ascending //按照开始时间排序
       select tuple;//映射
}
