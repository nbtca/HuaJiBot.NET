using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.UnitTest;

internal sealed class RecordingAdapter : TestAdapter
{
    /// <summary>What a non-Telegram adapter sends: posts replaced by their fallback.</summary>
    public readonly List<(string Target, SendingMessageBase[] Messages)> Sends = [];
    public readonly List<Post> Posts = [];

    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        lock (Posts)
            Posts.AddRange(messages.OfType<Post>());
        var sent = await messages.ExpandPostsAsync();
        lock (Sends)
            Sends.Add((targetGroup, sent));
        return ["42"];
    }

    public override Task<string[]> FeedbackAt(
        string? robotId,
        string targetGroup,
        string msgId,
        string text
    ) => SendGroupMessageAsync(robotId, targetGroup, new ReplyMessage(msgId), new TextMessage(text));
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
