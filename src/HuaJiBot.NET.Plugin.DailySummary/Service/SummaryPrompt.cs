using System.Text;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Utils;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

internal static class SummaryPrompt
{
    private const int HeaderReserve = 120;
    private const int MinimumChunkChars = 256;

    public static IReadOnlyList<string> BuildChunks(
        IReadOnlyList<GroupMessage> messages,
        int maxChars,
        DateTime date
    )
    {
        if (messages.Count == 0)
            return [];

        var limit = Math.Max(maxChars, MinimumChunkChars);
        var payloadLimit = limit - HeaderReserve;
        var payloads = new List<string>();
        var payload = new StringBuilder();

        void AddLine(string line)
        {
            if (payload.Length > 0 && payload.Length + line.Length + 1 > payloadLimit)
            {
                payloads.Add(payload.ToString());
                payload.Clear();
            }
            payload.Append(line).Append('\n');
        }

        foreach (var message in messages)
        {
            var prefix = $"[{Time(message.Timestamp)}] {(message.IsBot ? "机器人" : message.SenderName)}: ";
            var content = message.Content ?? string.Empty;
            var offset = 0;
            do
            {
                var linePrefix = offset == 0 ? prefix : prefix + "（续）";
                var capacity = payloadLimit - linePrefix.Length - 1;
                if (capacity < 2)
                    throw new InvalidOperationException("每日总结的分段字符上限过小，无法容纳消息头");
                var length = Math.Min(content.Length - offset, capacity);
                if (
                    length > 0
                    && offset + length < content.Length
                    && char.IsHighSurrogate(content[offset + length - 1])
                )
                    length--;
                AddLine(linePrefix + content.Substring(offset, length));
                offset += length;
            } while (offset < content.Length);
        }
        if (payload.Length > 0)
            payloads.Add(payload.ToString());

        return payloads
            .Select(
                (text, index) =>
                    $"以下是 {date:yyyy-MM-dd} 的群聊记录，第 {index + 1}/{payloads.Count} 段，按时间顺序连续切分。\n{text}"
            )
            .ToList();
    }

    public static IReadOnlyList<string> BuildMergeBatches(
        IReadOnlyList<string> summaries,
        int maxChars,
        DateTime date
    )
    {
        var batches = new List<string>();
        var batch = new StringBuilder();
        var payloadLimit = Math.Max(maxChars, MinimumChunkChars) - HeaderReserve;

        for (var i = 0; i < summaries.Count; i++)
        {
            var line = $"【分段摘要 {i + 1}】\n{summaries[i].Trim()}\n";
            if (batch.Length > 0 && batch.Length + line.Length > payloadLimit)
            {
                batches.Add(batch.ToString());
                batch.Clear();
            }
            batch.Append(line).Append('\n');
        }
        if (batch.Length > 0)
            batches.Add(batch.ToString());

        return batches
            .Select(
                (text, index) =>
                    $"以下是 {date:yyyy-MM-dd} 群聊的分段摘要，第 {index + 1}/{batches.Count} 组。请合并同类项。\n{text}"
            )
            .ToList();
    }

    public static string Time(DateTime timestamp) =>
        new DateTimeOffset(timestamp).ToOffset(NetworkTime.LocalTimeZoneOffset).ToString("HH:mm");

}
