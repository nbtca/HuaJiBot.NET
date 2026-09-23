using System.Text.RegularExpressions;
using HuaJiBot.NET.Bot;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace HuaJiBot.NET.Adapter.Telegram;

internal static partial class TelegramHtml
{
    internal const int CaptionLimit = 1024;
    internal const int TextLimit = 4096;

    internal static string Compose(Post post, int limit) =>
        Fit(Head(post), post.Markdown, limit, Render);

    internal static string ComposePlain(Post post, int limit) =>
        Fit(Head(post), post.Markdown, limit, x => Markdown.ToPlainText(x).Trim());

    internal static string Tag(string tag) => "#" + NonTagChars().Replace(tag, "_");

    internal static string Render(string markdown) =>
        string.Join("\n\n", Markdown.Parse(markdown).Select(Block).Where(x => x.Length > 0));

    private static string Head(Post post) =>
        post.Tags.Length == 0 ? "" : string.Join(' ', post.Tags.Select(Tag)) + "\n\n";

    private static string Fit(string head, string markdown, int limit, Func<string, string> render)
    {
        var text = head + render(markdown);
        for (var max = markdown.Length; text.Length > limit && max > 1;)
        {
            max -= text.Length - limit;
            text = head + render(Truncate(markdown, max));
        }
        return text;
    }

    private static string Truncate(string text, int max)
    {
        var end = Math.Max(max - 1, 0);
        if (end > 0 && char.IsLowSurrogate(text[end]))
            end--;
        return text[..end] + '…';
    }

    private static string Block(Block block) =>
        block switch
        {
            ParagraphBlock p => Inlines(p.Inline),
            HeadingBlock h => $"<b>{Inlines(h.Inline)}</b>",
            ListBlock list => string.Join(
                "\n",
                from ListItemBlock item in list
                select "• " + string.Join("\n", item.Select(Block))
            ),
            QuoteBlock quote => $"<blockquote>{string.Join("\n", quote.Select(Block))}</blockquote>",
            LeafBlock { Lines: var lines } leaf when leaf is CodeBlock =>
                $"<pre>{Escape(lines.ToString())}</pre>",
            LeafBlock { Lines: var lines } => Escape(lines.ToString()),
            ContainerBlock container => string.Join("\n", container.Select(Block)),
            _ => "",
        };

    private static string Inlines(ContainerInline? container) =>
        container is null ? "" : string.Concat(container.Select(Inline));

    private static string Inline(Markdig.Syntax.Inlines.Inline inline) =>
        inline switch
        {
            LiteralInline literal => Escape(literal.Content.ToString()),
            EmphasisInline { DelimiterCount: 2 } e => $"<b>{Inlines(e)}</b>",
            EmphasisInline e => $"<i>{Inlines(e)}</i>",
            CodeInline code => $"<code>{Escape(code.Content)}</code>",
            LinkInline { IsImage: true } link => Inlines(link),
            LinkInline link => $"<a href=\"{Escape(link.Url)}\">{Inlines(link)}</a>",
            AutolinkInline link => $"<a href=\"{Escape(link.Url)}\">{Escape(link.Url)}</a>",
            LineBreakInline => "\n",
            HtmlInline html => Escape(html.Tag),
            HtmlEntityInline entity => Escape(entity.Transcoded.ToString()),
            ContainerInline container => Inlines(container),
            _ => "",
        };

    internal static string Escape(string? text) =>
        (text ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    [GeneratedRegex(@"[^\p{L}\p{N}_]")]
    private static partial Regex NonTagChars();
}
