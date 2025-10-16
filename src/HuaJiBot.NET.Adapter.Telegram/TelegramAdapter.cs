using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HuaJiBot.NET.Adapter.Telegram;

public class TelegramAdapter(string botToken) : BotServiceBase
{
    private readonly TelegramBotClient _botClient = new(botToken);
    private readonly string _botToken = botToken;
    private CancellationTokenSource _cancellationTokenSource = new();

    public override required ILogger Logger { get; init; }

    private User? _botUser;

    public override void Reconnect()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new();
        _ = Task.Run(SetupServiceAsync);
    }

    public override async Task SetupServiceAsync()
    {
        try
        {
            _botUser = await _botClient.GetMe(_cancellationTokenSource.Token);
            await _botClient.DeleteWebhook(); // you may comment this line if you find it unnecessary
            await _botClient.DropPendingUpdates(); // you may comment this line if you find it unnecessary

            Log(
                $"Telegram bot started: @{_botUser.Username} ({_botUser.FirstName}) {_botUser.CanReadAllGroupMessages}"
            );
            Events.CallOnBotLogin(
                new()
                {
                    Service = this,
                    Accounts = [_botUser.Id.ToString()],
                    ClientName = "Telegram Bot",
                    ClientVersion = _botUser.Username,
                }
            );
            // Subscribe to events
            _botClient.OnMessage += HandleMessageAsync;
            _botClient.OnUpdate += HandleUpdateAsync;
            _botClient.OnError += (exception, source) =>
            {
                var errorMessage = exception switch
                {
                    ApiRequestException apiRequestException =>
                        $"Telegram API Error:\n[{apiRequestException.ErrorCode}] {apiRequestException.Message}",
                    _ => exception.ToString(),
                };

                LogError($"Telegram error from source {source}", errorMessage);
                return Task.CompletedTask;
            };

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
            // Group messages by type to combine text-based messages
            var textBuilder = new System.Text.StringBuilder();
            string? imagePathToSend = null;
            int? replyToMessageId = null;

            foreach (var message in messages)
            {
                switch (message)
                {
                    case TextMessage { Text: var text }:
                        if (textBuilder.Length > 0)
                            textBuilder.Append(' ');
                        // Escape HTML special characters
                        textBuilder.Append(
                            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                        );
                        break;

                    case AtMessage { Target: var target }
                        when long.TryParse(target, out var userId):
                        if (textBuilder.Length > 0)
                            textBuilder.Append(' ');
                        textBuilder.Append($"<a href=\"tg://user?id={userId}\">@{target}</a>");
                        break;

                    case ReplyMessage { MessageId: var msgId }
                        when int.TryParse(msgId, out var replyId):
                        replyToMessageId = replyId;
                        break;

                    case ImageMessage { ImagePath: var path }:
                        // If we have an image, we'll send it with the text as caption
                        imagePathToSend = path;
                        break;

                    default:
                        throw new NotSupportedException(
                            $"Message type {message.GetType()} is not supported"
                        );
                }
            }

            // Send the combined message
            Message? sentMessage = null;
            var combinedText = textBuilder.ToString();

            if (imagePathToSend != null)
            {
                // Send image with text as caption
                if (!string.IsNullOrWhiteSpace(combinedText))
                {
                    sentMessage = await _botClient.SendPhoto(
                        chatId,
                        InputFile.FromStream(System.IO.File.OpenRead(imagePathToSend)),
                        caption: combinedText,
                        parseMode: ParseMode.Html,
                        replyParameters: replyToMessageId.HasValue
                            ? new ReplyParameters { MessageId = replyToMessageId.Value }
                            : null,
                        cancellationToken: _cancellationTokenSource.Token
                    );
                }
                else
                {
                    sentMessage = await _botClient.SendPhoto(
                        chatId,
                        InputFile.FromStream(System.IO.File.OpenRead(imagePathToSend)),
                        replyParameters: replyToMessageId.HasValue
                            ? new ReplyParameters { MessageId = replyToMessageId.Value }
                            : null,
                        cancellationToken: _cancellationTokenSource.Token
                    );
                }
            }
            else if (!string.IsNullOrWhiteSpace(combinedText))
            {
                // Send text message
                sentMessage = await _botClient.SendMessage(
                    chatId,
                    combinedText,
                    parseMode: ParseMode.Html,
                    replyParameters: replyToMessageId.HasValue
                        ? new ReplyParameters { MessageId = replyToMessageId.Value }
                        : null,
                    cancellationToken: _cancellationTokenSource.Token
                );
            }

            if (sentMessage != null)
            {
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
                            new(targetGroup),
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
                        new(targetGroup),
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
                        new(targetGroup),
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

    private async Task HandleMessageAsync(Message message, UpdateType type)
    {
        try
        {
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

            LogDebug(
                $"Received message from {userName} in chat {chatId} (Type: {message.Chat.Type}): {messageText}"
            );

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
                LogDebug($"Handling group message in chat type: {message.Chat.Type}");

                var eventArgs = new GroupMessageEventArgs(
                    () => new DefaultCommandReader([messageText]),
                    async () =>
                    {
                        try
                        {
                            var chat = await _botClient.GetChat(
                                new(chatId),
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

                LogDebug($"Calling OnGroupMessageReceived event for chat {chatId}");
                Events.CallOnGroupMessageReceived(eventArgs);
            }
        }
        catch (Exception ex)
        {
            LogError("Error handling Telegram message", ex);
        }
    }

    private async Task HandleUpdateAsync(Update update)
    {
        try
        {
            LogDebug($"Received update: Type={update.Type}");

            // Handle non-message updates here if needed
            if (update.Message == null)
            {
                LogDebug($"Update has no message (Type: {update.Type})");
            }
        }
        catch (Exception ex)
        {
            LogError("Error handling Telegram update", ex);
        }
    }
}
