using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.UnitTest;

internal sealed class RecordingAdapter : TestAdapter
{
    public readonly List<(string Target, SendingMessageBase[] Messages)> Sends = [];

    public override Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        Sends.Add((targetGroup, messages));
        return Task.FromResult<string[]>(["42"]);
    }
}

internal sealed class RichAdapter : TestAdapter
{
    public readonly List<string> Targets = [];

    public override Task<string[]> SendRichMessageAsync(
        string? robotId,
        string targetGroup,
        RichContent content,
        Func<Task<SendingMessageBase[]>> fallback
    )
    {
        Targets.Add(targetGroup);
        return Task.FromResult<string[]>(["rich"]);
    }
}
