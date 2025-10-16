using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HuaJiBot.NET.Adapter.Telegram;

/// <summary>
/// 指令读取
/// </summary>
internal class TelegramCommandReader(BotService service, Message msg) : CommonCommandReader
{
    public override IEnumerable<ReaderEntity> Msg
    {
        get
        {
            return Parse();

            IEnumerable<ReaderEntity> Parse()
            {
                // Handle reply to message
                if (
                    msg.ReplyToMessage is { Type: not MessageType.ForumTopicCreated } replyToMessage
                )
                {
                    var replyContent =
                        replyToMessage.Text ?? replyToMessage.Caption ?? string.Empty;
                    yield return new ReaderReply(
                        new(
                            messageId: replyToMessage.MessageId.ToString(),
                            senderId: replyToMessage.From?.Id.ToString(),
                            content: replyContent
                        )
                    );
                }

                // Get the text content (from Text or Caption)
                var text = msg.Text ?? msg.Caption ?? string.Empty;

                // Parse entities if present
                if (msg.Entities is { Length: > 0 } entities)
                {
                    var lastIndex = 0;

                    foreach (var entity in entities)
                    {
                        // Yield text before this entity
                        if (entity.Offset > lastIndex)
                        {
                            var beforeText = text[lastIndex..entity.Offset];
                            if (!string.IsNullOrWhiteSpace(beforeText))
                            {
                                yield return beforeText;
                            }
                        }

                        // Handle the entity
                        switch (entity.Type)
                        {
                            case MessageEntityType.Mention: // @username
                                var mentionText = text.Substring(entity.Offset, entity.Length);
                                // For @username mentions, we don't have the user ID directly
                                // We'll yield it as text or try to extract from the message
                                yield return mentionText;
                                break;

                            case MessageEntityType.TextMention: // mention with user object
                                if (entity.User is { } user)
                                {
                                    yield return new ReaderAt(
                                        user.Id.ToString(),
                                        user.FirstName ?? user.Username ?? user.Id.ToString()
                                    );
                                }
                                break;

                            default:
                                // For other entity types, treat as regular text
                                var entityText = text.Substring(entity.Offset, entity.Length);
                                if (!string.IsNullOrWhiteSpace(entityText))
                                {
                                    yield return entityText;
                                }
                                break;
                        }

                        lastIndex = entity.Offset + entity.Length;
                    }

                    // Yield remaining text after last entity
                    if (lastIndex < text.Length)
                    {
                        var remainingText = text[lastIndex..];
                        if (!string.IsNullOrWhiteSpace(remainingText))
                        {
                            yield return remainingText;
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(text))
                {
                    // No entities, just yield the whole text
                    yield return text;
                }

                // Handle other message types that aren't text-based
                if (msg.Photo is { Length: > 0 })
                {
                    service.LogDebug("消息包含图片");
                }
                if (msg.Document is not null)
                {
                    service.LogDebug("消息包含文档");
                }
                if (msg.Sticker is not null)
                {
                    service.LogDebug("消息包含贴纸");
                }
            }
        }
    }
}
