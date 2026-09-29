using HuaJiBot.NET.Commands;
using Kook;
using Kook.WebSocket;

namespace HuaJiBot.NET.Adapter.Kook.Messaging;

/// <summary>
/// 将 KOOK 网关消息按结构拆分为文本 / At / 回复 / 图片(URL) 序列，保留原始顺序。
/// 图片以 URL 作为文本片段参与命令解析（框架命令实体不含图片类型）。
/// </summary>
internal class KookCommandReader(SocketUserMessage message) : CommonCommandReader
{
    public override IEnumerable<ReaderEntity> Msg => Parse(message);

    /// <summary>
    /// 供适配器生成 TextMessage：面向人的可读展示，与命令解析（纯 URL / 结构化）分离。
    /// </summary>
    public static string ToReadableText(SocketUserMessage message) =>
        KookMessageRenderer.ToReadableText(message);

    private static IEnumerable<ReaderEntity> Parse(SocketUserMessage message)
    {
        if (message.Quote is { QuotedMessageId: var quotedId } && quotedId != Guid.Empty)
        {
            var quoteDetail = message.Quote as Quote;
            yield return new ReaderReply(
                new(
                    messageId: quotedId.ToString(),
                    senderId: quoteDetail?.Author?.Id.ToString(),
                    content: quoteDetail?.Content
                )
            );
        }

        if (message.MaybeTextImageMixedMessage())
        {
            foreach (var entity in ParseMixedCard(message))
                yield return entity;
            yield break;
        }

        foreach (var entity in ParseContentWithMentions(message.Content ?? string.Empty, message))
            yield return entity;

        // KOOK 附件位于正文之后。
        foreach (var attachment in message.Attachments)
        {
            if (attachment.Type is AttachmentType.Image && !string.IsNullOrEmpty(attachment.Url))
                yield return new ReaderText(attachment.Url);
        }
    }

    private static IEnumerable<ReaderEntity> ParseContentWithMentions(
        string content,
        SocketUserMessage message
    )
    {
        var userMentionTags = message
            .Tags.Where(x => x.Type is TagType.UserMention)
            .OrderBy(x => x.Index)
            .ToArray();

        var cursor = 0;
        foreach (var tag in userMentionTags)
        {
            if (tag.Index < cursor || tag.Index > content.Length)
                continue;

            if (tag.Index > cursor)
            {
                var text = content[cursor..tag.Index];
                if (!string.IsNullOrEmpty(text))
                    yield return new ReaderText(text);
            }

            var (atTarget, atName) = tag.Value switch
            {
                SocketGuildUser guildUser => (guildUser.Id.ToString(), guildUser.DisplayName),
                IUser user => (user.Id.ToString(), user.Username),
                _ when tag.Key is ulong id => (id.ToString(), (string?)null),
                _ => (null, null),
            };
            if (atTarget is not null)
                yield return new ReaderAt(atTarget, atName is null ? null : "@" + atName);

            cursor = Math.Min(tag.Index + tag.Length, content.Length);
        }

        if (cursor < content.Length)
        {
            var text = content[cursor..];
            if (!string.IsNullOrEmpty(text))
                yield return new ReaderText(text);
        }
    }

    private static IEnumerable<ReaderEntity> ParseMixedCard(SocketUserMessage message)
    {
        foreach (var module in message.Cards.OfType<Card>().SelectMany(x => x.Modules))
        {
            switch (module)
            {
                case SectionModule { Text: KMarkdownElement { Content: var content } }:
                    foreach (var entity in ParseContentWithMentions(content, message))
                        yield return entity;
                    break;
                case SectionModule { Text: PlainTextElement { Content: var content } }:
                    if (!string.IsNullOrEmpty(content))
                        yield return new ReaderText(content);
                    break;
                case ContainerModule { Elements: var elements }:
                    foreach (var element in elements)
                        if (!string.IsNullOrEmpty(element.Source))
                            yield return new ReaderText(element.Source);
                    break;
                case ImageGroupModule { Elements: var elements }:
                    foreach (var element in elements)
                        if (!string.IsNullOrEmpty(element.Source))
                            yield return new ReaderText(element.Source);
                    break;
                case IMediaModule { Source: { Length: > 0 } source }:
                    yield return new ReaderText(source);
                    break;
            }
        }
    }
}
