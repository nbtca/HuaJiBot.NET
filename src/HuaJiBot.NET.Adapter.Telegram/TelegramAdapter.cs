using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HuaJiBot.NET.Adapter.Telegram;

public class TelegramAdapter : BotServiceBase
{
    private readonly TelegramBotClient _botClient;
    private readonly string _botToken;
    private CancellationTokenSource _cancellationTokenSource = new();

    public override required ILogger Logger { get; init; }

    private User? _botUser;

    public TelegramAdapter(string botToken)
    {
        _botToken = botToken;
        _botClient = new TelegramBotClient(botToken);
    }

    public override void Reconnect()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(SetupServiceAsync);
    }

    public override async Task SetupServiceAsync()
    {
        try
        {
            _botUser = await _botClient.GetMe(_cancellationTokenSource.Token);
            Log($"Telegram bot started: @{_botUser.Username} ({_botUser.FirstName})");

            Events.CallOnBotLogin(
                new BotLoginEventArgs
                {
                    Service = this,
                    Accounts = [_botUser.Id.ToString()],
                    ClientName = "Telegram Bot",
                    ClientVersion = _botUser.Username,
                }
            );

            // Start receiving updates
            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                cancellationToken: _cancellationTokenSource.Token
            );

            Log("Telegram bot is receiving messages...");
        }
        catch (Exception ex)
        {
            LogError("Failed to setup Telegram service", ex);
            throw;
        }
    }

    public override string[] AllRobots => _botUser != null ? [_botUser.Id.ToString()] : [];

    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        var chatId = new ChatId(targetGroup);
        var messageIds = new List<string>();

        try
        {
            foreach (var message in messages)
            {
                var sentMessage = message switch
                {
                    TextMessage { Text: var text } => await _botClient.SendMessage(
                        chatId,
                        text,
                        parseMode: ParseMode.Html, // Support HTML formatting
                        cancellationToken: _cancellationTokenSource.Token
                    ),

                    ImageMessage { ImagePath: var path } => await _botClient.SendPhoto(
                        chatId,
                        InputFile.FromStream(System.IO.File.OpenRead(path)),
                        cancellationToken: _cancellationTokenSource.Token
                    ),

                    ReplyMessage { MessageId: var msgId }
                        when int.TryParse(msgId, out var replyToId) => await _botClient.SendMessage(
                        chatId,
                        "",
                        replyParameters: int.Parse(msgId),
                        cancellationToken: _cancellationTokenSource.Token
                    ),

                    AtMessage { Target: var target } when long.TryParse(target, out var userId) =>
                        await _botClient.SendMessage(
                            chatId,
                            $"<a href=\"tg://user?id={userId}\">@{target}</a>", // Proper Telegram mention
                            parseMode: ParseMode.Html,
                            cancellationToken: _cancellationTokenSource.Token
                        ),

                    _ => throw new NotSupportedException(
                        $"Message type {message.GetType()} is not supported"
                    ),
                };

                messageIds.Add(sentMessage.MessageId.ToString());
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to send message to chat {targetGroup}", ex);
            throw;
        }

        return messageIds.ToArray();
    }

    public override void RecallMessage(string? robotId, string targetGroup, string msgId)
    {
        try
        {
            if (int.TryParse(msgId, out var messageId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _botClient.DeleteMessage(
                            new ChatId(targetGroup),
                            messageId,
                            _cancellationTokenSource.Token
                        );
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to delete message {msgId}", ex);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to recall message {msgId}", ex);
        }
    }

    public override void SetGroupName(string? robotId, string targetGroup, string groupName)
    {
        try
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _botClient.SetChatTitle(
                        new ChatId(targetGroup),
                        groupName,
                        _cancellationTokenSource.Token
                    );
                }
                catch (Exception ex)
                {
                    LogError($"Failed to set group name for {targetGroup}", ex);
                }
            });
        }
        catch (Exception ex)
        {
            LogError($"Failed to set group name for {targetGroup}", ex);
        }
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        try
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    var member = await _botClient.GetChatMember(
                        new ChatId(targetGroup),
                        long.Parse(userId),
                        _cancellationTokenSource.Token
                    );

                    return member.Status switch
                    {
                        ChatMemberStatus.Creator => MemberType.Owner,
                        ChatMemberStatus.Administrator => MemberType.Admin,
                        ChatMemberStatus.Member => MemberType.Member,
                        _ => MemberType.Unknown,
                    };
                }
                catch
                {
                    return MemberType.Unknown;
                }
            });

            return task.Result;
        }
        catch
        {
            return MemberType.Unknown;
        }
    }

    public override string GetNick(string robotId, string userId)
    {
        try
        {
            // For Telegram, we typically get user info through chat member calls
            // This is a simplified version - in practice you'd cache this information
            if (long.TryParse(userId, out var telegramUserId))
            {
                // Return the user ID as fallback since we can't easily get user info without a chat context
                return userId;
            }
            return userId;
        }
        catch
        {
            return userId;
        }
    }

    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data"));
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (update.Message is not { } message)
                return;

            var chatId = message.Chat.Id.ToString();
            var userId = message.From?.Id.ToString() ?? "";
            var userName = message.From?.FirstName ?? "";

            // Handle different types of messages
            string messageText =
                message.Text
                ?? message.Caption
                ?? (message.Photo?.Length > 0 ? "[Photo]" : "")
                ?? (message.Document != null ? $"[Document: {message.Document.FileName}]" : "")
                ?? (message.Sticker != null ? "[Sticker]" : "")
                ?? (message.Voice != null ? "[Voice]" : "")
                ?? (message.Audio != null ? "[Audio]" : "")
                ?? (message.Video != null ? "[Video]" : "")
                ?? "[Unknown message type]";

            LogDebug($"Received message from {userName} in chat {chatId}: {messageText}");

            // Determine if this is a group or private chat
            if (message.Chat.Type == ChatType.Private)
            {
                // Handle private message
                var privateEventArgs = new PrivateMessageEventArgs(() =>
                    new DefaultCommandReader([messageText])
                )
                {
                    Service = this,
                    RobotId = _botUser?.Id.ToString(),
                    GroupId = null, // Private chats don't have group ID
                    SenderId = userId,
                    MessageId = message.MessageId.ToString(),
                    TextMessageLazy = new(() => messageText),
                };

                Events.CallOnPrivateMessageReceived(privateEventArgs);
            }
            else
            {
                // Handle group message
                var eventArgs = new GroupMessageEventArgs(
                    () => new DefaultCommandReader([messageText]),
                    async () =>
                    {
                        try
                        {
                            var chat = await _botClient.GetChat(
                                new ChatId(chatId),
                                _cancellationTokenSource.Token
                            );
                            return chat.Title ?? chat.FirstName ?? chatId;
                        }
                        catch
                        {
                            return chatId;
                        }
                    }
                )
                {
                    Service = this,
                    RobotId = _botUser?.Id.ToString(),
                    GroupId = chatId,
                    SenderId = userId,
                    SenderMemberCard = userName,
                    MessageId = message.MessageId.ToString(),
                    TextMessageLazy = new(() => messageText),
                };

                Events.CallOnGroupMessageReceived(eventArgs);
            }
        }
        catch (Exception ex)
        {
            LogError("Error handling Telegram update", ex);
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString(),
        };

        LogError("Telegram polling error", errorMessage);
        return Task.CompletedTask;
    }
}
