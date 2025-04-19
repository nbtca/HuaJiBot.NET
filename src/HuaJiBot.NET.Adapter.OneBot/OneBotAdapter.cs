using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Adapter.OneBot;

public class OneBotAdapter : BotServiceBase
{
    private readonly ForwardWebSocketClient _client;

    public OneBotAdapter(string ws, string? token)
    {
        _client = new ForwardWebSocketClient(this, ws, token);
    }

    public override required ILogger Logger { get; init; }

    public override void Reconnect()
    {
        _client.ConnectAsync();
    }

    public override Task SetupServiceAsync() => _client.ConnectAsync();

    public override string[] AllRobots => _client.QQ is not null ? [_client.QQ] : [];

    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        var result = await _client.Api.SendGroupMessageAsync(
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
                        AtMessage { Target: var target } => new AtMessageEntity(uint.Parse(target)),
                        ReplyMessage { MessageId: var id } => new ReplyMessageEntity(id),
                        _ => throw new NotSupportedException(),
                    }
                )
                .ToArray()
        );
        return [result.MessageId.ToString()];
    }

    public override void RecallMessage(string? robotId, string targetGroup, string msgId)
    {
        _client.Api.RecallMessageAsync(targetGroup, msgId);
    }

    public override void SetGroupName(string? robotId, string targetGroup, string groupName)
    {
        _client.Api.SetGroupNameAsync(targetGroup, groupName);
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override async Task<string[]> FeedbackAt(
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

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 取插件数据目录
    /// </summary>
    /// <returns></returns>
    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data")); //插件数据目录，当前目录下的plugins/data
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path); //自动创建目录
        return path;
    }
}
