using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;

namespace HuaJiBot.NET.Plugin.AIChat.Plugins;

/// <summary>
/// A basic plugin that provides utility functions like getting the current time
/// to be used by Semantic Kernel.
/// </summary>
public sealed class DateTimeUtils
{
    [KernelFunction]
    [Description("Get the current date and time")]
    public string GetCurrentDateTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    [KernelFunction]
    [Description("Get the current date")]
    public string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    [KernelFunction]
    [Description("Get the current time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    [KernelFunction]
    [Description("Get the current day of week")]
    public string GetDayOfWeek()
    {
        return DateTime.Now.DayOfWeek.ToString();
    }

    [KernelFunction]
    [Description("Calculate time difference between two timestamps")]
    public string CalculateTimeDifference(
        [Description("First timestamp in format yyyy-MM-dd HH:mm:ss")] string timestamp1,
        [Description("Second timestamp in format yyyy-MM-dd HH:mm:ss")] string timestamp2
    )
    {
        if (
            DateTime.TryParseExact(
                timestamp1,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt1
            )
            && DateTime.TryParseExact(
                timestamp2,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt2
            )
        )
        {
            var diff = dt2 - dt1;
            return $"{diff.Days} days, {diff.Hours} hours, {diff.Minutes} minutes, {diff.Seconds} seconds";
        }

        return "Invalid timestamp format. Use yyyy-MM-dd HH:mm:ss.";
    }
}
