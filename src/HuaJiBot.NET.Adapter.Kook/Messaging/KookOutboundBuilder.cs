using System.Text;
using HuaJiBot.NET.Bot;
using Kook;

namespace HuaJiBot.NET.Adapter.Kook.Messaging;

/// <summary>
/// 将 HuaJiBot 的消息段序列翻译为 KOOK 发送原语：
/// 文本 / At / 链接合并为 KMarkdown 段，图片单独成段，引用作用于其后第一条消息。
/// 只做"消息段 -> 发送原语"的映射，不负责实际发送（由适配器编排）。
/// </summary>
internal static class KookOutboundBuilder
{
    public static IReadOnlyList<KookOutboundSegment> Build(IEnumerable<SendingMessageBase> messages)
    {
        var segments = new List<KookOutboundSegment>();
        var textBuilder = new StringBuilder();
        IQuote? quote = null;

        void FlushText()
        {
            if (textBuilder.Length == 0)
                return;
            segments.Add(new KookTextSegment(textBuilder.ToString(), quote));
            textBuilder.Clear();
            quote = null; // 引用只作用于其后的第一条消息
        }

        foreach (var message in messages)
        {
            switch (message)
            {
                case TextMessage { Text: var text }:
                    textBuilder.Append(text.Sanitize());
                    break;

                case AtMessage { Target: var target }:
                    textBuilder.Append(
                        ulong.TryParse(target, out var atId)
                            ? MentionUtils.KMarkdownMentionUser(atId)
                            : target.Sanitize()
                    );
                    break;

                case LinkMessage { Text: var text, Url: var url }:
                    textBuilder.Append(Format.Url(text, url));
                    break;

                case ReplyMessage { MessageId: var msgId }
                    when Guid.TryParse(msgId, out var quotedId):
                    quote = new MessageReference(quotedId);
                    break;

                case ImageMessage { ImagePath: var path }:
                    // 图片单独成条，引用（若有）随图片发出。
                    FlushText();
                    segments.Add(new KookImageSegment(path, quote));
                    quote = null;
                    break;

                default:
                    throw new NotSupportedException(
                        $"KOOK 适配器不支持的消息类型：{message.GetType().Name}"
                    );
            }
        }

        FlushText();
        return segments;
    }

    /// <summary>
    /// 从本地路径或 URL 提取文件名（KOOK 附件需要文件名）。
    /// </summary>
    public static string GetImageFileName(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            var name = Path.GetFileName(uri.AbsolutePath);
            return string.IsNullOrEmpty(name) ? "image.png" : name;
        }
        return Path.GetFileName(path);
    }

    /// <summary>
    /// 图片路径为 http(s) 链接时复用 KOOK asset，否则作为本地文件上传。
    /// </summary>
    public static FileAttachment CreateImageAttachment(string path)
    {
        var fileName = GetImageFileName(path);
        return Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? new FileAttachment(uri, fileName, AttachmentType.Image)
            : new FileAttachment(path, fileName, AttachmentType.Image);
    }
}
