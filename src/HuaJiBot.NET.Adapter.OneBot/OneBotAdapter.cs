using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Adapter.OneBot;

public class OneBotAdapter : BotServiceBase
{
    private readonly ForwardWebSocketClient _client;

    public OneBotAdapter(string ws, string? token)
    {
        _client = new ForwardWebSocketClient(this, ws, token);
    }

    public override void Reconnect()
    {
        _client.ConnectAsync();
    }

    public override Task SetupServiceAsync() => _client.ConnectAsync();

    public override string[] GetAllRobots()
    {
        if (_client.QQ is not null)
            return [_client.QQ];
        return [];
    }

    public override void SendGroupMessage(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        _ = _client
            .Api
            .SendGroupMessageAsync(
                targetGroup,
                messages
                    .Select<SendingMessageBase, MessageEntity>(
                        x =>
                            x switch
                            {
                                TextMessage { Text: var text } => new TextMessageEntity(text),
                                ImageMessage { ImagePath: var path }
                                    => new ImageMessageEntity
                                    {
                                        File = CommonResolver.EncodingBase64Async(path)
                                    },
                                AtMessage { Target: var target }
                                    => new AtMessageEntity(uint.Parse(target)),
                                ReplyMessage
                                {
                                    ReplayMsgSeq: var seq,
                                    ReplyMsgId: var id,
                                    Target: var target
                                }
                                    => new ReplyMessageEntity(uint.Parse(id)),
                                _ => throw new NotSupportedException()
                            }
                    )
                    .ToArray()
            );
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string? robotId, string targetGroup, string userId, string text)
    {
        SendGroupMessage(robotId, targetGroup, new AtMessage(userId), new TextMessage(text));
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 日志
    /// </summary>
    /// <param name="message">日志内容</param>
    public override void Log(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}");
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    /// <param name="message"></param>
    public override void Warn(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}");
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    /// <param name="message">调试日志内容</param>
    public override void LogDebug(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] {message}");
    }

    /// <summary>
    /// 错误日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="detail">错误信息</param>
    public override void LogError(object message, object detail)
    {
        Console.WriteLine(
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}"
                + $"{Environment.NewLine}---{Environment.NewLine}"
                + $"{detail}"
                + $"{Environment.NewLine}---"
        );
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
