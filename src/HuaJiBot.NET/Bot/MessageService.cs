namespace HuaJiBot.NET.Bot;

public abstract class MessageService : IMessageService
{
    public abstract Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    );

    public abstract void RecallMessage(string? robotId, string targetGroup, string msgId);

    public abstract void SetGroupName(string? robotId, string targetGroup, string groupName);

    public virtual async Task<string[]> FeedbackAt(
        string? robotId,
        string targetGroup,
        string msgId,
        string text
    )
    {
        return await SendGroupMessageAsync(
            robotId,
            targetGroup,
            new ReplyMessage(msgId),
            new TextMessage(text)
        );
    }
}
