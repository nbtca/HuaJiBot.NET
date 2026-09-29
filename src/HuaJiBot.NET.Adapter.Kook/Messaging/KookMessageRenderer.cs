using Kook;
using Kook.WebSocket;

namespace HuaJiBot.NET.Adapter.Kook.Messaging;

/// <summary>
/// 将 KOOK 消息渲染为面向人的可读文本，与命令解析（结构化 reader）职责分离。
/// 卡片按 Card -> Module -> Element 渲染为树形；媒体消息列出附件基本信息。
/// </summary>
internal static class KookMessageRenderer
{
    public static string ToReadableText(SocketUserMessage message)
    {
        if (message.Type is MessageType.Card || message.MaybeTextImageMixedMessage())
            return DescribeCards(message);

        if (message.Attachments.Count > 0)
            return string.Join(" ", message.Attachments.Select(DescribeAttachment));

        return message.Resolve();
    }

    private static string DescribeAttachment(IAttachment attachment)
    {
        var type = attachment.Type switch
        {
            AttachmentType.Image => "图片",
            AttachmentType.Video => "视频",
            AttachmentType.Audio => "音频",
            AttachmentType.File => "文件",
            _ => "附件",
        };
        var name = string.IsNullOrEmpty(attachment.Filename) ? null : attachment.Filename;
        return name is null ? $"[{type}] {attachment.Url}" : $"[{type}] {name} {attachment.Url}";
    }

    /// <summary>
    /// 将卡片消息渲染为 Card -> Module -> Element 的树形结构。
    /// </summary>
    private static string DescribeCards(SocketUserMessage message)
    {
        var lines = new List<string>();
        foreach (var card in message.Cards.OfType<Card>())
        {
            lines.Add("[卡片]");
            AppendModules(card.Modules, string.Empty, lines);
        }
        return string.Join("\n", lines);
    }

    private static void AppendModules(
        IReadOnlyList<IModule> modules,
        string prefix,
        List<string> lines
    )
    {
        for (var i = 0; i < modules.Count; i++)
        {
            var last = i == modules.Count - 1;
            DescribeModule(modules[i], prefix, last, lines);
        }
    }

    private static void DescribeModule(
        IModule module,
        string prefix,
        bool last,
        List<string> lines
    )
    {
        var self = prefix + Branch(last);
        var childPrefix = prefix + Indent(last);
        switch (module)
        {
            case HeaderModule { Text.Content: var content }:
                lines.Add($"{self}[标题] {content}");
                break;
            case SectionModule { Text: var text, Accessory: var accessory }:
                lines.Add($"{self}[内容]");
                var children = new List<IElement>();
                if (text is not null)
                    children.Add(text);
                if (accessory is not null)
                    children.Add(accessory);
                AppendElements(children, childPrefix, lines);
                break;
            case ContainerModule { Elements: var elements }:
                lines.Add($"{self}[容器]");
                AppendElements(elements.Cast<IElement>().ToArray(), childPrefix, lines);
                break;
            case ImageGroupModule { Elements: var elements }:
                lines.Add($"{self}[图片组]");
                AppendElements(elements.Cast<IElement>().ToArray(), childPrefix, lines);
                break;
            case ActionGroupModule { Elements: var elements }:
                lines.Add($"{self}[交互]");
                AppendElements(elements.Cast<IElement>().ToArray(), childPrefix, lines);
                break;
            case ContextModule { Elements: var elements }:
                lines.Add($"{self}[备注]");
                AppendElements(elements, childPrefix, lines);
                break;
            case FileModule { Source: var source, Title: var title }:
                lines.Add($"{self}{Media("文件", title, source)}");
                break;
            case VideoModule { Source: var source, Title: var title }:
                lines.Add($"{self}{Media("视频", title, source)}");
                break;
            case AudioModule { Source: var source, Title: var title }:
                lines.Add($"{self}{Media("音频", title, source)}");
                break;
            case CountdownModule:
                lines.Add($"{self}[倒计时]");
                break;
            case InviteModule:
                lines.Add($"{self}[邀请]");
                break;
            case DividerModule:
                lines.Add($"{self}[分割线]");
                break;
            default:
                lines.Add($"{self}[{module.Type}]");
                break;
        }
    }

    private static void AppendElements(
        IReadOnlyList<IElement> elements,
        string prefix,
        List<string> lines
    )
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var last = i == elements.Count - 1;
            DescribeElement(elements[i], prefix, last, lines);
        }
    }

    private static void DescribeElement(
        IElement element,
        string prefix,
        bool last,
        List<string> lines
    )
    {
        var self = prefix + Branch(last);
        switch (element)
        {
            case PlainTextElement { Content: var content }:
                lines.Add($"{self}[普通文本] {content}");
                break;
            case KMarkdownElement { Content: var content }:
                lines.Add($"{self}[KMarkdown] {content}");
                break;
            case ImageElement { Source: var source }:
                lines.Add($"{self}{Media("图片", null, source)}");
                break;
            case ButtonElement { Text: var text }:
                lines.Add($"{self}[按钮] {ElementText(text)}");
                break;
            case ParagraphStruct { Fields: var fields }:
                lines.Add($"{self}[区域文本]");
                AppendElements(fields, prefix + Indent(last), lines);
                break;
            default:
                lines.Add($"{self}[{element.Type}]");
                break;
        }
    }

    private static string ElementText(IElement element) =>
        element switch
        {
            PlainTextElement { Content: var content } => content,
            KMarkdownElement { Content: var content } => content,
            _ => element.Type.ToString(),
        };

    /// <summary>
    /// [类型] 文本 链接（无标题时省略文本）。
    /// </summary>
    private static string Media(string type, string? title, string source) =>
        string.IsNullOrEmpty(title) ? $"[{type}] {source}" : $"[{type}] {title} {source}";

    private static string Branch(bool last) => last ? "└─ " : "├─ ";

    private static string Indent(bool last) => last ? "   " : "│  ";
}
