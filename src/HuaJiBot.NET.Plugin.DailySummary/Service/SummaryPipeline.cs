using HuaJiBot.NET.DataBase;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

internal static class SummaryPipeline
{
    private const int MergePromptChars = 12000;
    private const int MaxMergeRounds = 12;

    private const string BriefInstructions =
        "输出简体中文纯文本简报，约 200 至 350 个汉字。先写【今日要点】，用“• ”列出 2 至 4 条重要话题、进展或决定；确有明确约定时再写【待办】，最多 2 条。每条独占一行，句子简短。不要使用 Markdown、长篇背景、未经证实的原因或自行推断的任务、负责人及优先级。只依据输入内容。";

    private const string PartInstructions =
        "这是全天记录的一段，请提取至多 4 条具体要点，总长不超过 180 个汉字。只保留该段的事实、明确决定和确有依据的待办；不要写完整日报，不要推测其他时段。";

    private const string MergeInstructions =
        "请将这些按时间顺序排列的分段摘要合并去重，保留重要事实和明确决定，总长不超过 350 个汉字。不要加入输入中没有的信息。";

    public static async Task<string> RunAsync(
        IReadOnlyList<GroupMessage> messages,
        DateTime date,
        int maxPromptChars,
        string systemPrompt,
        Func<string, string, CancellationToken, Task<string>> invoke,
        CancellationToken cancellationToken
    )
    {
        var chunks = SummaryPrompt.BuildChunks(messages, maxPromptChars, date);
        if (chunks.Count == 0)
            throw new InvalidOperationException("没有可总结的消息");
        if (chunks.Count == 1)
            return await invoke(systemPrompt + "\n" + BriefInstructions, chunks[0], cancellationToken);

        var summaries = new List<string>(chunks.Count);
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            summaries.Add(await invoke(systemPrompt + "\n" + PartInstructions, chunk, cancellationToken));
        }

        for (var round = 0; round < MaxMergeRounds; round++)
        {
            var batches = SummaryPrompt.BuildMergeBatches(summaries, MergePromptChars, date);
            var merged = new List<string>(batches.Count);
            foreach (var batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var instructions = batches.Count == 1
                    ? systemPrompt + "\n" + BriefInstructions
                    : systemPrompt + "\n" + MergeInstructions;
                merged.Add(await invoke(instructions, batch, cancellationToken));
            }
            if (merged.Count == 1)
                return merged[0];
            summaries = merged;
        }
        throw new InvalidOperationException("分段摘要过多，无法在限定轮数内合并");
    }
}
