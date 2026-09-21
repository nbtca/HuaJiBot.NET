using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Adapter.OneBot;

public class OneBotAdapter(string ws, string? token) : BotServiceBase
{
    // Created lazily: the init-only Logger is still null while the constructor runs.
    private ForwardWebSocketClient Client => field ??= new(this, ws, token);

    public override required ILogger Logger { get; init; }

    protected override void ReconnectCore()
    {
        Client.ConnectAsync();
    }

    protected override Task SetupServiceAsyncCore() => Client.StartAsync();

    public override string[] AllRobots => Client.QQ is not null ? [Client.QQ] : [];

    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        var result = await Client.Api.SendGroupMessageAsync(
            targetGroup,
            messages
                .Select<SendingMessageBase, MessageEntity>(x =>
                    x switch
                    {
                        TextMessage { Text: var text } => new TextMessageEntity(text),
                        ImageMessage { ImagePath: var path } => new ImageMessageEntity
                        {
                            File = CommonResolver.EncodingBase64Async(path),
                        },
                        AtMessage { Target: var target } => new AtMessageEntity(target),
                        ReplyMessage { MessageId: var id } => new ReplyMessageEntity(id),
                        _ => throw new NotSupportedException(),
                    }
                )
                .ToArray()
        );
        return [result.MessageId.ToString()];
    }

    public override void RecallMessage(string? robotId, string targetGroup, string msgId) =>
        LogFailure(Client.Api.RecallMessageAsync(msgId), $"撤回消息 {msgId} 失败");

    protected override void SetGroupNameCore(string? robotId, string targetGroup, string groupName) =>
        LogFailure(Client.Api.SetGroupNameAsync(targetGroup, groupName), $"修改群 {targetGroup} 名称失败");

    private async void LogFailure(Task task, string message)
    {
        try
        {
            await task;
        }
        catch (Exception e)
        {
            LogError(message, e);
        }
    }

    protected override MemberType GetMemberTypeCore(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    protected override string GetNickCore(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

}
