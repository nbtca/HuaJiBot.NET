using System.Text;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Utils;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

internal static class SummaryPrompt
{
    public sealed record Result(string Text, bool Truncated, DateTime? KeptFrom);

    /// <summary>
    /// 按时间顺序拼装发给模型的用户消息。超出 maxChars 时保留最新的消息，
    /// 并在提示中明确告知模型记录已被截断。
    /// </summary>
    public static Result Build(
        IReadOnlyList<GroupMessage> orderedMessages,
        int maxChars,
        DateTime targetDate
    )
    {
        var offset = NetworkTime.LocalTimeZoneOffset;
        var lines = orderedMessages
            .Select(m => (m.Timestamp, Text: FormatLine(m, offset)))
            .ToArray();

        var kept = new List<(DateTime Timestamp, string Text)>();
        var used = 0;
        var truncated = false;
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var cost = lines[i].Text.Length + 1;
            if (kept.Count > 0 && used + cost > maxChars)
            {
                truncated = true;
                break;
            }
            kept.Add(lines[i]);
            used += cost;
        }
        kept.Reverse();

        var sb = new StringBuilder();
        if (truncated && kept.Count > 0)
        {
            var keptFrom = new DateTimeOffset(kept[0].Timestamp)
                .ToOffset(offset)
                .ToString("HH:mm");
            sb.AppendLine(
                $"注意：当日消息过多，记录已截断，以下仅包含 {keptFrom} 之后的 {kept.Count} 条消息，更早的内容未包含在内。\n"
            );
        }
        sb.AppendLine($"以下是 {targetDate:yyyy-MM-dd} 的群聊记录，请进行总结：\n");
        foreach (var (_, text) in kept)
            sb.AppendLine(text);

        return new Result(sb.ToString(), truncated, truncated ? kept[0].Timestamp : null);
    }

    private static string FormatLine(GroupMessage m, TimeSpan offset)
    {
        var time = new DateTimeOffset(m.Timestamp).ToOffset(offset).ToString("HH:mm");
        var sender = m.IsBot ? "机器人" : m.SenderName;
        return $"[{time}] {sender}: {m.Content}";
    }
}
