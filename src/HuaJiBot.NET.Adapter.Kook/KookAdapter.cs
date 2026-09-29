using HuaJiBot.NET.Adapter.Kook.Channels;
using HuaJiBot.NET.Adapter.Kook.Messaging;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;
using Kook;
using Kook.WebSocket;

namespace HuaJiBot.NET.Adapter.Kook;

/// <summary>
/// KOOK 协议适配器。
/// </summary>
/// <remarks>
/// 对接 KOOK 开放平台的 Bot。
/// </remarks>
public class KookAdapter : BotServiceBase
{
    private readonly KookSocketClient _client;
    private readonly string _token;

    /// <inheritdoc />
    public override required ILogger Logger { get; init; }

    /// <summary>
    /// 使用指定的 Bot 令牌创建 KOOK 适配器。
    /// </summary>
    /// <param name="token"> KOOK 机器人令牌。 </param>
    /// <param name="config">
    /// 可选的 KOOK 客户端配置。为 <see langword="null"/> 时使用适配器默认配置。
    /// </param>
    public KookAdapter(string token, KookSocketConfig? config = null)
    {
        _token = token;
        _client = new KookSocketClient(
            config
                ?? new KookSocketConfig
                {
                    // KOOK 不允许同一 Bot 多处在线，默认情况下登录前先退出旧连接以确保能收到网关事件。
                    AutoLogoutBeforeLogin = true,
                }
        );

        _client.Log += OnClientLog;
        _client.Ready += OnReadyAsync;
        _client.MessageReceived += OnMessageReceivedAsync;
        _client.DirectMessageReceived += OnDirectMessageReceivedAsync;
    }

    #region 生命周期

    protected override void ReconnectCore() => _ = ReconnectAsync();

    protected override Task SetupServiceAsyncCore() => ConnectAsync();

    private async Task ConnectAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    private async Task ReconnectAsync()
    {
        try
        {
            await _client.StopAsync();
            await ConnectAsync();
        }
        catch (Exception e)
        {
            LogError("KOOK 重新连接失败", e);
        }
    }

    private Task OnClientLog(LogMessage msg)
    {
        var text = $"[KOOK] {msg.Source}: {msg.Message}";
        switch (msg.Severity)
        {
            case LogSeverity.Critical or LogSeverity.Error:
                LogError(text, msg.Exception);
                break;
            case LogSeverity.Warning:
                Warn(text);
                break;
            case LogSeverity.Debug or LogSeverity.Verbose:
                LogDebug(text);
                break;
            default:
                Log(text);
                break;
        }
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        var self = _client.CurrentUser;
        Events.CallOnBotLogin(
            new BotLoginEventArgs
            {
                Service = this,
                Accounts = self is null ? [] : [self.Id.ToString()],
                ClientName = "KOOK",
                ClientVersion = self?.Username,
            }
        );
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override string[] AllRobots =>
        _client.CurrentUser is { } self ? [self.Id.ToString()] : [];

    #endregion

    #region 接收消息

    private Task OnMessageReceivedAsync(
        SocketMessage message,
        SocketGuildUser author,
        SocketTextChannel channel
    )
    {
        try
        {
            var self = _client.CurrentUser;

            if (self is not null && author.Id == self.Id)
                return Task.CompletedTask;

            if (message is not SocketUserMessage userMessage)
                return Task.CompletedTask;

            Events.CallOnGroupMessageReceived(
                new GroupMessageEventArgs(
                    () => new KookCommandReader(userMessage),
                    () => new ValueTask<string>(channel.Guild.Name + "#" + channel.Name)
                )
                {
                    Service = this,
                    RobotId = self?.Id.ToString(),
                    MessageId = message.Id.ToString(),
                    GroupId = channel.Id.ToString(),
                    SenderId = author.Id.ToString(),
                    SenderMemberCard = author.DisplayName,
                    SenderMemberType = ResolveMemberType(author),
                    TextMessageLazy = new(() => KookCommandReader.ToReadableText(userMessage)),
                }
            );
        }
        catch (Exception e)
        {
            LogError("处理 KOOK 消息失败", e);
        }
        return Task.CompletedTask;
    }

    private Task OnDirectMessageReceivedAsync(
        SocketMessage message,
        SocketUser author,
        SocketDMChannel channel
    )
    {
        try
        {
            var self = _client.CurrentUser;

            if (self is not null && author.Id == self.Id)
                return Task.CompletedTask;

            if (message is not SocketUserMessage userMessage)
                return Task.CompletedTask;

            Events.CallOnPrivateMessageReceived(
                new PrivateMessageEventArgs(() => new KookCommandReader(userMessage))
                {
                    Service = this,
                    RobotId = self?.Id.ToString(),
                    MessageId = message.Id.ToString(),
                    GroupId = channel.ChatCode.ToString(),
                    SenderId = author.Id.ToString(),
                    TextMessageLazy = new(() => KookCommandReader.ToReadableText(userMessage)),
                }
            );
        }
        catch (Exception e)
        {
            LogError("处理 KOOK 私聊消息失败", e);
        }
        return Task.CompletedTask;
    }

    private static MemberType ResolveMemberType(SocketGuildUser user)
    {
        if (user.IsOwner == true || user.Guild.OwnerId == user.Id)
            return MemberType.Owner;
        if (user.GuildPermissions.Administrator)
            return MemberType.Admin;
        return MemberType.Member;
    }

    private async Task<IMessageChannel?> ResolveMessageChannelAsync(KookChannelId channelId) =>
        channelId.Kind switch
        {
            KookChannelKind.Guild => _client.GetChannel(channelId.GuildChannelId) as IMessageChannel,
            KookChannelKind.Direct => await _client.GetDMChannelAsync(channelId.DirectChatCode),
            _ => throw new ArgumentOutOfRangeException(
                nameof(channelId),
                channelId.Kind,
                "未知的 KOOK 频道种类"
            ),
        };

    private async Task<IMessageChannel?> ResolveMessageChannelAsync(string targetGroup) =>
        KookChannelId.TryParse(targetGroup, out var channelId)
            ? await ResolveMessageChannelAsync(channelId)
            : null;

    #endregion

    #region 发送 / 撤回

    /// <inheritdoc />
    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        messages = await messages.ExpandPostsAsync();

        if (await ResolveMessageChannelAsync(targetGroup) is not { } channel)
            throw new InvalidOperationException($"未找到 KOOK 频道 {targetGroup}");

        var messageIds = new List<string>();
        foreach (var segment in KookOutboundBuilder.Build(messages))
        {
            switch (segment)
            {
                case KookTextSegment { Markdown: var markdown, Quote: var quote }:
                    var text = await channel.SendTextAsync(markdown, quote);
                    messageIds.Add(text.Id.ToString());
                    break;

                case KookImageSegment { ImagePath: var path, Quote: var quote }:
                    using (var attachment = KookOutboundBuilder.CreateImageAttachment(path))
                    {
                        var image = await channel.SendFileAsync(attachment, quote);
                        messageIds.Add(image.Id.ToString());
                    }
                    break;
            }
        }

        return messageIds.ToArray();
    }

    /// <inheritdoc />
    /// <remarks>
    /// KOOK 原生支持 KMarkdown。此实现将 <see cref="RichContent.Markdown"/> 作为 KMarkdown 原样发送，
    /// 使加粗、链接、提及、表情等富文本得以正确渲染；因此内容不会被转义，调用方需自行保证 KMarkdown 合法。
    /// </remarks>
    public override async Task<string[]> SendRichMessageAsync(
        string? robotId,
        string targetGroup,
        RichContent content,
        Func<Task<SendingMessageBase[]>> fallback
    )
    {
        if (await ResolveMessageChannelAsync(targetGroup) is not { } channel)
            throw new InvalidOperationException($"未找到 KOOK 频道 {targetGroup}");

        IQuote? quote =
            content.ReplyToMessageId is { } replyTo && Guid.TryParse(replyTo, out var quotedId)
                ? new MessageReference(quotedId)
                : null;

        var result = await channel.SendTextAsync(content.Markdown, quote);
        return [result.Id.ToString()];
    }

    /// <inheritdoc />
    public override void RecallMessage(string? robotId, string targetGroup, string msgId)
    {
        if (!Guid.TryParse(msgId, out var messageId))
        {
            LogError($"撤回消息失败，非法消息 ID：{msgId}", null);
            return;
        }
        LogFailure(RecallAsync(), $"撤回消息 {msgId} 失败");
        return;

        async Task RecallAsync()
        {
            if (await ResolveMessageChannelAsync(targetGroup) is not { } channel)
            {
                LogError($"撤回消息失败，未找到频道：{targetGroup}", null);
                return;
            }
            await channel.DeleteMessageAsync(messageId);
        }
    }

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

    #endregion

    #region 群（频道）管理

    protected override void SetGroupNameCore(string? robotId, string targetGroup, string groupName)
    {
        if (!KookChannelId.TryParse(targetGroup, out var channelId))
            throw new ArgumentException($"非法的 KOOK 频道标识：{targetGroup}", nameof(targetGroup));

        switch (channelId.Kind)
        {
            case KookChannelKind.Guild:
                if (_client.GetChannel(channelId.GuildChannelId) is not ITextChannel channel)
                {
                    LogError($"修改频道名失败，未找到文字频道：{targetGroup}", null);
                    return;
                }
                LogFailure(
                    channel.ModifyAsync(p => p.Name = groupName),
                    $"修改频道 {targetGroup} 名称失败"
                );
                break;

            case KookChannelKind.Direct:
                throw new NotSupportedException("KOOK 私聊频道不支持修改名称");

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(targetGroup),
                    channelId.Kind,
                    "未知的 KOOK 频道种类"
                );
        }
    }

    protected override MemberType GetMemberTypeCore(string robotId, string targetGroup, string userId)
    {
        if (!ulong.TryParse(userId, out var uid) || !KookChannelId.TryParse(targetGroup, out var channelId))
            return MemberType.Unknown;

        switch (channelId.Kind)
        {
            case KookChannelKind.Guild:
                if (_client.GetChannel(channelId.GuildChannelId) is not SocketTextChannel channel)
                    return MemberType.Unknown;
                var guild = channel.Guild;
                // 群主判定不依赖用户缓存：OwnerId 始终可用；管理员判定需用户已缓存，否则不臆造。
                if (guild.OwnerId == uid)
                    return MemberType.Owner;
                return guild.GetUser(uid) is { } user ? ResolveMemberType(user) : MemberType.Member;

            case KookChannelKind.Direct:
                // 私聊没有成员类型概念。
                return MemberType.Unknown;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(targetGroup),
                    channelId.Kind,
                    "未知的 KOOK 频道种类"
                );
        }
    }

    protected override string GetNickCore(string robotId, string userId)
    {
        if (ulong.TryParse(userId, out var uid) && _client.GetUser(uid) is { } user)
            return user.Username;
        return userId;
    }

    #endregion
}
