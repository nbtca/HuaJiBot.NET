﻿using System.Text;
using HuaJiBot.NET.Utils;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HuaJiBot.NET.Plugin.Calendar;

internal static class CalendarExtensions
{
    public class Period(Ical.Net.DataTypes.Period p)
    {
        public DateTimeOffset StartTime { get; } =
            p.StartTime.AsDateTimeOffset.ToOffset(NetworkTime.LocalTimeZoneOffset);
        public DateTimeOffset EndTime { get; } =
            p.EndTime.AsDateTimeOffset.ToOffset(NetworkTime.LocalTimeZoneOffset);
    }

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
        DateTimeOffset start,
        DateTimeOffset end
    )
    {
        //ical.net时区bug，故使用两个时区获取
        //https://github.com/rianjs/ical.net/issues/569
        var occWithTimeZone = @this.GetOccurrences(
            new CalDateTime(
                start.ToOffset(NetworkTime.LocalTimeZoneOffset).DateTime,
                "Asia/Shanghai"
            ),
            new CalDateTime(end.ToOffset(NetworkTime.LocalTimeZoneOffset).DateTime, "Asia/Shanghai")
        ); //+8时区的事件
        var occUtc = @this.GetOccurrences(
            new CalDateTime(start.UtcDateTime, "UTC"),
            new CalDateTime(end.UtcDateTime, "UTC")
        ); //UTC时区的事件
        var allOcc = occWithTimeZone.Concat(occUtc).Distinct(); //合并并去重
        return from occurrence in allOcc
            select //选择
            occurrence.Source switch
            {
                CalendarEvent calendarEvent
                    => (Period: new Period(occurrence.Period), calendarEvent),
                _
                    => throw new ArgumentOutOfRangeException(
                        "not impl " + occurrence.Source.GetType()
                    )
            } into tuple
            orderby tuple.Period.StartTime ascending //按照开始时间排序
            where //确保时间范围内
                tuple.Period.StartTime < end && tuple.Period.EndTime > start
            select tuple;
        //映射
    }

    //todo: 生成图片
    //public static CardBuilder.AutoDeleteFile BuildCardOutput(
    //    this IEnumerable<(Period period, CalendarEvent e)> @this,
    //    DateTime now
    //)
    //{
    //}

    /// <summary>
    /// 输出日程的文本
    /// </summary>
    /// <param name="this">日程迭代枚举</param>
    /// <param name="now">当前时间</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string BuildTextOutput(
        this IEnumerable<(Period period, CalendarEvent e)> @this,
        DateTimeOffset now
    )
    {
        StringBuilder sb = new();
        foreach (var (period, ev) in @this) //遍历每一个事件
        {
            string FormatTimeWeek(DateTimeOffset date)
            {
                var weekStr = date.DayOfWeek switch
                {
                    DayOfWeek.Monday => "一",
                    DayOfWeek.Tuesday => "二",
                    DayOfWeek.Wednesday => "三",
                    DayOfWeek.Thursday => "四",
                    DayOfWeek.Friday => "五",
                    DayOfWeek.Saturday => "六",
                    DayOfWeek.Sunday => "日",
                    _ => throw new ArgumentOutOfRangeException(nameof(date.DayOfWeek))
                };
                var dateOffset = date;
                var weekInfo = "";
                //判断是否是当前周
                var beginOfNextWeek = now.Date.AddDays(7 - (int)now.DayOfWeek);
                var beginOfThisWeek = beginOfNextWeek.AddDays(-7);
                if (dateOffset.Date >= beginOfThisWeek && dateOffset.Date < beginOfNextWeek)
                    weekInfo = $"周{weekStr}";
                else
                { //判断是否是下一周
                    beginOfNextWeek = beginOfNextWeek.AddDays(7);
                    beginOfThisWeek = beginOfThisWeek.AddDays(7);
                    if (dateOffset.Date >= beginOfThisWeek && dateOffset.Date < beginOfNextWeek)
                        weekInfo = $"下周{weekStr}";
                    else
                    { //判断是否是上一周
                        beginOfNextWeek = beginOfNextWeek.AddDays(-14);
                        beginOfThisWeek = beginOfThisWeek.AddDays(-14);
                        if (dateOffset.Date >= beginOfThisWeek && dateOffset.Date < beginOfNextWeek)
                            weekInfo = $"上周{weekStr}";
                    }
                }

                if (dateOffset.Date == now.Date)
                    return $"{dateOffset:MM-dd} 今天 {weekInfo}  {dateOffset:HH:mm}";
                if (dateOffset.Date == now.Date.AddDays(1))
                    return $"{dateOffset:MM-dd} 明天 {weekInfo} {dateOffset:HH:mm}";
                if (dateOffset.Date == now.Date.AddDays(2))
                    return $"{dateOffset:MM-dd} 后天 {weekInfo} {dateOffset:HH:mm}";
                if (dateOffset.Year == now.Year) //same year
                    return $"{dateOffset:MM-dd} {weekInfo} {dateOffset:HH:mm}";
                return $"{dateOffset:yyyy-MM-dd} {weekInfo} {dateOffset:HH:mm}";
            }
            //判断是同一天
            if (period.StartTime.Date != period.EndTime.Date)
            {
                sb.AppendLine(
                    $"{FormatTimeWeek(period.StartTime)} ~ {FormatTimeWeek(period.EndTime)}"
                ); //输出
            }
            else
            {
                sb.AppendLine($"{FormatTimeWeek(period.StartTime)} ~ {period.EndTime:HH:mm}"); //输出
            }
            if (!string.IsNullOrWhiteSpace(ev.Summary))
                sb.AppendLine($"    概要：{ev.Summary}");
            if (!string.IsNullOrWhiteSpace(ev.Location))
                sb.AppendLine($"    地点：{ev.Location}");
            if (!string.IsNullOrWhiteSpace(ev.Description))
            {
                var desc = ev.Description.Trim();
                // trim ev.Description after 50 characters
                if (desc.Length > 50)
                    desc = desc[..50] + " ...";
                desc = desc.Replace("\r", null).Replace("\n\n", "\n").Replace("\n", "\n    ");
                sb.AppendLine($"    描述：{desc}");
            }
        }

        return sb.ToString();
    }
}
