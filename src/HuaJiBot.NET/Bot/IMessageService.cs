namespace HuaJiBot.NET.Bot;

public interface IMessageService
{
    Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    );
    Task<string[]> SendRichMessageAsync(
        string? robotId,
        string targetGroup,
        RichContent content,
        Func<Task<SendingMessageBase[]>> fallback
    );
    void RecallMessage(string? robotId, string targetGroup, string msgId);
    void SetGroupName(string? robotId, string targetGroup, string groupName);
    Task<string[]> FeedbackAt(string? robotId, string targetGroup, string msgId, string text);
}
