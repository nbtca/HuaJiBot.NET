using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using Kook;
using static HuaJiBot.NET.Commands.CommonCommandReader;

namespace HuaJiBot.NET.Adapter.Kook;

/// <summary>
/// Kook 指令读取器
/// </summary>
internal class KookCommandReader(BotService service, IMessage message) : CommonCommandReader
{
    public override IEnumerable<ReaderEntity> Msg
    {
        get
        {
            return Parse();

            IEnumerable<ReaderEntity> Parse()
            {
                // Parse the message content
                var content = message.Content;
                
                // For now, just return the text content
                // TODO: Parse mentions, replies, and other Kook-specific content
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }

                // Handle mentions
                if (message is IUserMessage userMessage && userMessage.MentionedUsers.Any())
                {
                    foreach (var mentionedUser in userMessage.MentionedUsers)
                    {
                        yield return new ReaderAt(mentionedUser.Id.ToString(), mentionedUser.Username);
                    }
                }

                // Handle quote/reply if reference exists
                if (message.Reference.HasValue && message.Reference.Value.MessageId.HasValue)
                {
                    var refMessageId = message.Reference.Value.MessageId.Value.ToString();
                    yield return new ReaderReply(new CommandReader.ReplyInfo(
                        messageId: refMessageId,
                        seqId: null,
                        senderId: null,
                        content: null
                    ));
                }

                // TODO: Handle other Kook-specific message types like cards, embeds, etc.
            }
        }
    }
}