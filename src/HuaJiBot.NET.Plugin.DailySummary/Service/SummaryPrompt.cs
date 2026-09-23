using System.Text;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Utils;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

internal static class SummaryPrompt
{
    /// <param name="KeptFrom">Time of the oldest kept message when older ones were cut, else null.</param>
    public sealed record Result(string Text, DateTime? KeptFrom);

    public static Result Build(IReadOnlyList<GroupMessage> messages, int maxChars, DateTime date)
    {
        var start = messages.Count;
        for (var used = 0; start > 0; start--)
        {
            used += Format(messages[start - 1]).Length + 1;
            if (used > maxChars && start < messages.Count)
                break;
        }

        var sb = new StringBuilder();
        DateTime? keptFrom = start > 0 ? messages[start].Timestamp : null;
        if (keptFrom is { } from)
            sb.AppendLine(
                $"注意：当日消息过多，记录已截断，以下仅包含 {Time(from)} 之后的 {messages.Count - start} 条消息。\n"
            );
        sb.AppendLine($"以下是 {date:yyyy-MM-dd} 的群聊记录，请进行总结：\n");
        for (var i = start; i < messages.Count; i++)
            sb.AppendLine(Format(messages[i]));
        return new Result(sb.ToString(), keptFrom);
    }

    public static string Time(DateTime timestamp) =>
        new DateTimeOffset(timestamp).ToOffset(NetworkTime.LocalTimeZoneOffset).ToString("HH:mm");

    private static string Format(GroupMessage m) =>
        $"[{Time(m.Timestamp)}] {(m.IsBot ? "机器人" : m.SenderName)}: {m.Content}";
}
