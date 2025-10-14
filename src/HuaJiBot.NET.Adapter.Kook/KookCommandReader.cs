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
                // Parse the message content - Kook uses KMarkdown format
                var content = message.Content;
                
                if (string.IsNullOrEmpty(content))
                    yield break;

                // Parse mentions in the format (met)userId(met)
                var mentionPattern = @"\(met\)(\d+)\(met\)";
                var matches = System.Text.RegularExpressions.Regex.Matches(content, mentionPattern);
                
                var processedContent = content;
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var userId = match.Groups[1].Value;
                    processedContent = processedContent.Replace(match.Value, "");
                    
                    // Get the username if possible
                    var username = "";
                    if (message is IUserMessage userMessage && userMessage.MentionedUsers.Any())
                    {
                        var mentionedUser = userMessage.MentionedUsers.FirstOrDefault(u => u.Id.ToString() == userId);
                        username = mentionedUser?.Username ?? "";
                    }
                    
                    yield return new ReaderAt(userId, username);
                }

                // Return the text content without mentions
                var trimmedContent = processedContent.Trim();
                if (!string.IsNullOrEmpty(trimmedContent))
                {
                    yield return trimmedContent;
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

                // TODO: Handle other Kook-specific message types like cards, embeds, attachments, etc.
                // Kook supports rich KMarkdown content that could be parsed here
            }
        }
    }
}