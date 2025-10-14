using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;
using Kook;
using Kook.WebSocket;

namespace HuaJiBot.NET.Adapter.Kook;

public class KookAdapter : BotServiceBase
{
    private readonly KookSocketClient _client;
    private readonly string _token;

    public KookAdapter(string token)
    {
        _token = token;
        _client = new KookSocketClient();
    }

    public override required ILogger Logger { get; init; }

    public override void Reconnect()
    {
        _ = Task.Run(async () =>
        {
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                await _client.StopAsync();
            }
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        });
    }

    public override async Task SetupServiceAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        
        _client.MessageReceived += OnMessageReceived;
        _client.Ready += OnReady;
    }

    private Task OnReady()
    {
        Events.CallOnBotLogin(this, new BotLoginEventArgs
        {
            Service = this,
            Accounts = _client.CurrentUser is not null ? [_client.CurrentUser.Id.ToString()] : [],
            ClientName = "Kook.Net",
            ClientVersion = "0.0.45-alpha"
        });
        Log($"Kook Bot 登录成功！账号：{_client.CurrentUser?.Username}({_client.CurrentUser?.Id})");
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(Cacheable<IMessage, Guid> cachedMessage, ISocketMessageChannel channel)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var message = cachedMessage.HasValue ? cachedMessage.Value : await cachedMessage.GetOrDownloadAsync();
                
                // Only process messages from guilds (servers)
                if (channel is not ITextChannel textChannel || message is not IUserMessage userMessage)
                    return;

                // Ignore bot messages
                if (message.Author.IsBot)
                    return;

                var guild = textChannel.Guild;
                var author = message.Author;

                // Create command reader for the message
                var commandReader = new KookCommandReader(this, message);

                // Create group message event args
                var eventArgs = new GroupMessageEventArgs(
                    () => commandReader,
                    () => ValueTask.FromResult(textChannel.Name)
                )
                {
                    Service = this,
                    RobotId = _client.CurrentUser?.Id.ToString(),
                    MessageId = message.Id.ToString(),
                    GroupId = textChannel.Id.ToString(),
                    SenderId = author.Id.ToString(),
                    SenderMemberCard = author.Username,
                    TextMessageLazy = new Lazy<string>(() => message.Content)
                };

                // Call the group message received event
                Events.CallOnGroupMessageReceived(eventArgs);
            }
            catch (Exception ex)
            {
                LogError("处理消息时出错", ex);
            }
        });
        
        return Task.CompletedTask;
    }

    public override string[] AllRobots => _client.CurrentUser is not null 
        ? [_client.CurrentUser.Id.ToString()] 
        : [];

    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        if (!ulong.TryParse(targetGroup, out var channelId))
            throw new ArgumentException("Invalid channel ID format", nameof(targetGroup));

        var channel = await _client.GetChannelAsync(channelId) as ITextChannel;
        if (channel == null)
            throw new ArgumentException("Channel not found", nameof(targetGroup));

        var messageIds = new List<string>();

        foreach (var message in messages)
        {
            switch (message)
            {
                case TextMessage textMessage:
                    var sentMessage = await channel.SendTextAsync(textMessage.Text);
                    messageIds.Add(sentMessage.Id.ToString());
                    break;
                case AtMessage atMessage:
                    if (ulong.TryParse(atMessage.Target, out var userId))
                    {
                        var sentAtMessage = await channel.SendTextAsync($"(met){userId}(met)");
                        messageIds.Add(sentAtMessage.Id.ToString());
                    }
                    break;
                case ImageMessage imageMessage:
                    // TODO: Implement image sending
                    LogDebug($"图片消息暂未实现: {imageMessage.ImagePath}");
                    break;
                case ReplyMessage replyMessage:
                    // TODO: Implement reply message
                    LogDebug($"回复消息暂未实现: {replyMessage.MessageId}");
                    break;
                default:
                    LogDebug($"未支持的消息类型: {message.GetType().Name}");
                    break;
            }
        }

        return messageIds.ToArray();
    }

    public override void RecallMessage(string? robotId, string targetGroup, string msgId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!ulong.TryParse(targetGroup, out var channelId) || !Guid.TryParse(msgId, out var messageId))
                    return;

                var channel = await _client.GetChannelAsync(channelId) as ITextChannel;
                if (channel == null)
                    return;

                var message = await channel.GetMessageAsync(messageId);
                if (message != null)
                {
                    await message.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                LogError("撤回消息失败", ex);
            }
        });
    }

    public override void SetGroupName(string? robotId, string targetGroup, string groupName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!ulong.TryParse(targetGroup, out var channelId))
                    return;

                var channel = await _client.GetChannelAsync(channelId) as ITextChannel;
                if (channel == null)
                    return;

                await channel.ModifyAsync(x => x.Name = groupName);
            }
            catch (Exception ex)
            {
                LogError("修改频道名称失败", ex);
            }
        });
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        // TODO: Implement member type retrieval based on Kook roles
        return MemberType.Member;
    }

    public override async Task<string[]> FeedbackAt(
        string? robotId,
        string targetGroup,
        string msgId,
        string text
    )
    {
        if (!ulong.TryParse(targetGroup, out var channelId))
            return [];

        var channel = await _client.GetChannelAsync(channelId) as ITextChannel;
        if (channel == null)
            return [];

        try
        {
            // Try to get the original message to find the sender
            if (Guid.TryParse(msgId, out var messageId))
            {
                var originalMessage = await channel.GetMessageAsync(messageId);
                if (originalMessage != null)
                {
                    // Create a mention for the original sender
                    var mention = $"(met){originalMessage.Author.Id}(met)";
                    var replyText = $"{mention} {text}";
                    var sentMessage = await channel.SendTextAsync(replyText);
                    return [sentMessage.Id.ToString()];
                }
            }

            // Fallback: just send the text without mention
            var fallbackMessage = await channel.SendTextAsync(text);
            return [fallbackMessage.Id.ToString()];
        }
        catch (Exception ex)
        {
            LogError("发送回复消息失败", ex);
            return [];
        }
    }

    public override string GetNick(string robotId, string userId)
    {
        // TODO: Implement nickname retrieval
        return "";
    }

    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data"));
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
}