using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HuaJiBot.NET.Adapter.Telegram;

public class TelegramAdapter(string botToken) : BotServiceBase
{
    private static readonly HttpClient RichMessageClient = new();
    private readonly TelegramBotClient _botClient = new(botToken);
    private CancellationTokenSource _cancellationTokenSource = new();

    public override required ILogger Logger { get; init; }

    private User? _botUser;

    protected override void ReconnectCore()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new();
        _ = Task.Run(SetupServiceAsyncCore);
    }

    protected override async Task SetupServiceAsyncCore()
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

    internal static ReplyParameters? ToReplyParameters(string? messageId) =>
        int.TryParse(messageId, out var id) ? new ReplyParameters { MessageId = id } : null;

    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        var groupTopic = GroupTopic.Parse(targetGroup);
        var chatId = new ChatId(groupTopic.GroupId);
        var topicId = groupTopic.TopicId;
        var messageIds = new List<string>();

        try
        {
            // Group messages by type to combine text-based messages
            var textBuilder = new System.Text.StringBuilder();
            string? imagePathToSend = null;
            ReplyParameters? replyParameters = null;

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
                        when ToReplyParameters(msgId) is { } reply:
                        replyParameters = reply;
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
                        replyParameters: replyParameters,
                        messageThreadId: topicId,
                        cancellationToken: _cancellationTokenSource.Token
                    );
                }
                else
                {
                    sentMessage = await _botClient.SendPhoto(
                        chatId,
                        InputFile.FromStream(System.IO.File.OpenRead(imagePathToSend)),
                        replyParameters: replyParameters,
                        messageThreadId: topicId,
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
                    replyParameters: replyParameters,
                    messageThreadId: topicId,
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

    public override async Task<string[]> SendRichMessageAsync(
        string? robotId,
        string targetGroup,
        RichContent content,
        Func<Task<SendingMessageBase[]>> fallback
    )
    {
        var groupTopic = GroupTopic.Parse(targetGroup);
        try
        {
            var replyParameters = ToReplyParameters(content.ReplyToMessageId);
            using var response = await RichMessageClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{botToken}/sendRichMessage",
                new
                {
                    chat_id = groupTopic.GroupId,
                    rich_message = new { markdown = content.Markdown },
                    reply_parameters = replyParameters is { } reply
                        ? new { message_id = reply.MessageId }
                        : null,
                    message_thread_id = groupTopic.TopicId,
                },
                _cancellationTokenSource.Token
            );
            var result = await response.Content.ReadFromJsonAsync<RichMessageResponse>(
                _cancellationTokenSource.Token
            );
            if (!response.IsSuccessStatusCode || result is not { Ok: true, Result: { } message })
            {
                throw new HttpRequestException(result?.Description ?? response.ReasonPhrase);
            }
            return [message.MessageId.ToString()];
        }
        catch (Exception ex)
        {
            LogError($"Failed to send rich message to chat {targetGroup}", ex);
            throw;
        }
    }

    private sealed record RichMessageResponse(
        bool Ok,
        RichMessageResult? Result,
        string? Description
    );

    private sealed record RichMessageResult([property: JsonPropertyName("message_id")] int MessageId);

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

    protected override void SetGroupNameCore(string? robotId, string targetGroup, string groupName)
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

    protected override MemberType GetMemberTypeCore(string robotId, string targetGroup, string userId)
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

    protected override string GetNickCore(string robotId, string userId)
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

    private async Task HandleMessageAsync(Message message, UpdateType type)
    {
        try
        {
            var groupTopic = new GroupTopic(
                message.Chat.Id,
                message.IsTopicMessage ? message.MessageThreadId : null
            );

            var userId = message.From?.Id.ToString() ?? "";
            var userName = $"{message.From?.FirstName} {message.From?.LastName}";

            string messageText =
                message.Text ?? message.Caption ?? (message.Photo?.Length > 0 ? "[Photo]" : "");

            LogDebug(
                $"Received message from {userName} in chat {groupTopic} (Type: {message.Chat.Type}): {messageText}"
            );

            // Determine if this is a group or private chat
            if (message.Chat.Type == ChatType.Private)
            {
                // Handle private message
                var privateEventArgs = new PrivateMessageEventArgs(() =>
                    new TelegramCommandReader(this, message)
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
                    () => new TelegramCommandReader(this, message),
                    async () =>
                    {
                        try
                        {
                            var chat = (
                                await _botClient.GetChat(
                                    new(groupTopic.GroupId),
                                    _cancellationTokenSource.Token
                                )
                            );
                            var chatName = chat.Title ?? chat.FirstName ?? groupTopic;
                            if (message.ReplyToMessage?.ForumTopicCreated?.Name is { } forumTopic)
                            {
                                return chatName + "#" + forumTopic;
                            }
                            return chatName;
                        }
                        catch
                        {
                            return groupTopic;
                        }
                    }
                )
                {
                    Service = this,
                    RobotId = _botUser?.Id.ToString(),
                    GroupId = groupTopic,
                    SenderId = userId,
                    SenderMemberCard = userName,
                    MessageId = message.MessageId.ToString(),
                    TextMessageLazy = new(() => messageText),
                };

                LogDebug($"Calling OnGroupMessageReceived event for chat {groupTopic}");
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
