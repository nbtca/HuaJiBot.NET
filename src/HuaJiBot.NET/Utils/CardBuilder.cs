using System.Numerics;
using System.Text;
using HuaJiBot.NET.Utils.Fonts;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Path = System.IO.Path;

namespace HuaJiBot.NET.Utils;

public abstract class ImageBuilder
{
    #region TextRuns

    private static int getCodePointLength(string text)
    {
        //// https://github.com/SixLabors/Fonts/blob/main/src/SixLabors.Fonts/Unicode/UnicodeUtility.cs#L645
        // var pattern = @"[\uD800-\uDBFF][\uDC00-\uDFFF]";
        //var regex = new Regex(pattern);
        //var count = regex.Matches(text).Count;
        //return text.Length - count;
        //https://github.com/SixLabors/Fonts/blob/d74f3fae7250cf3a76f43780abea6e15ec40b75e/src/SixLabors.Fonts/TextRun.cs#L55
        var chars = 0;
        SpanGraphemeEnumerator graphemeEnumerator = new(text);
        while (graphemeEnumerator.MoveNext())
        {
            //SpanCodePointEnumerator codePointEnumerator = new(graphemeEnumerator.Current);
            //while (codePointEnumerator.MoveNext())
            //{
            //    //chars += codePointEnumerator.Current.Utf16SequenceLength;
            //    Console.WriteLine(" + " + codePointEnumerator.Current.Utf16SequenceLength);
            chars++;
            //}
            //Console.WriteLine(graphemeEnumerator.Current.);
        }
        return chars;
    }

    /// <summary>
    /// 将富文本转换为ImageSharp的RichTextRun
    /// </summary>
    /// <param name="runs"></param>
    /// <returns></returns>
    protected static (string text, IReadOnlyList<RichTextRun> runs) BuildTextRuns(
        IEnumerable<TextRun> runs
    )
    {
        var sb = new StringBuilder();
        var currentIndex = 0;
        var list = new List<RichTextRun>();

        var count = 0;
        var defaultFont = new Lazy<Font>(() => FontManager.MaoKenTangYuan.CreateFont(20));
        var boldFont = new Lazy<Font>(() => FontManager.MaoKenTangYuanBold.CreateFont(20));
        var italicFont = new Lazy<Font>(() => FontManager.MaoKenTangYuanItalic.CreateFont(20));
        var boldItalicFont = new Lazy<Font>(
            () => FontManager.MaoKenTangYuanItalicBold.CreateFont(20)
        );
        var prefix = string.Empty;
        foreach (var line in runs)
        {
            if (line.Text == string.Empty)
                continue; //空字符串
            if (count++ > 200) //防止死循环
                break;
            if (line.StartPrefix)
            {
                prefix += line.Text;
                continue;
            }
            if (line.EndPrefix)
            {
                if (prefix.Length > line.Text.Length)
                    prefix = prefix[..^line.Text.Length];
                continue;
            }
            var (text, color, font) = line;
            var run = new RichTextRun
            {
                Brush = new SolidBrush(color),
                Start = currentIndex,
                End = currentIndex += getCodePointLength(text),
                Font = font,
                TextAttributes = line.TextAttributes,
            };
            if (line.Underline)
                run.TextDecorations = TextDecorations.Underline;
            if (line.Italic || line.Bold)
            {
                if (line is { Italic: true, Bold: true })
                    run.Font = boldItalicFont.Value;
                else if (line.Italic)
                    run.Font = italicFont.Value;
                else if (line.Bold)
                    run.Font = boldFont.Value;
            }
            if (line.Strikethrough)
                run.TextDecorations |= TextDecorations.Strikeout;
            if (line.Olive)
                run.TextDecorations |= TextDecorations.Overline;
            if (line.FontSize is { } newFontSize)
            {
                run.Font ??= defaultFont.Value;
                run.Font = new Font(
                    run.Font.Family,
                    newFontSize,
                    (run.Font.IsItalic ? FontStyle.Italic : FontStyle.Regular)
                        | (run.Font.IsBold ? FontStyle.Bold : FontStyle.Regular)
                );
            }
            sb.Append(text);
            list.Add(run);
        }
        return (sb.ToString(), list);
    }
    #endregion
    #region ProcessAvatarWithRoundedCorner

    //用于图片圆角生成
    //ref from https://github.com/SixLabors/Samples/blob/main/ImageSharp/AvatarWithRoundedCorner/Program.cs#L52
    // This method can be seen as an inline implementation of an `IImageProcessor`:
    // (The combination of `IImageOperations.Apply()` + this could be replaced with an `IImageProcessor`)
    protected static IImageProcessingContext ApplyRoundedCorners(
        IImageProcessingContext context,
        float cornerRadius
    )
    {
        var size = context.GetCurrentSize();
        var corners = BuildCorners(size.Width, size.Height, cornerRadius);
        context.SetGraphicsOptions(
            new GraphicsOptions
            {
                Antialias = true,
                // Enforces that any part of this shape that has color is punched out of the background
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut,
            }
        );

        // Mutating in here as we already have a cloned original
        // use any color (not Transparent), so the corners will be clipped
        foreach (var path in corners)
        {
            context = context.Fill(Color.Red, path);
        }
        return context;
    }

    private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
    {
        // First create a square
        var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);
        // Then cut out of the square a circle so we are left with a corner
        var cornerTopLeft = rect.Clip(
            new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius)
        );
        // Corner is now a corner shape positions top left
        // let's make 3 more positioned correctly, we can do that by translating the original around the center of the image.
        var rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
        var bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;
        // Move it across the width of the image - the width of the shape
        var cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
        var cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
        var cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);
        return new PathCollection(
            cornerTopLeft,
            cornerBottomLeft,
            cornerTopRight,
            cornerBottomRight
        );
    }
    #endregion



    /// <summary>
    /// 保存到临时文件
    /// </summary>
    /// <returns>自动删除文件</returns>
    public TempFile.AutoDeleteFile SaveTempAutoDelete(bool autoHeight = false)
    {
        var tempFile = new TempFile.AutoDeleteFile(Path.GetTempFileName());
        try
        {
            Generate(tempFile, autoHeight);
            return tempFile;
        }
        catch
        {
            tempFile.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 生成图像
    /// 并直接保存到文件
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <param name="autoHeight">自动测量高度</param>
    public void Generate(string file, bool autoHeight = false)
    {
        using var stream = File.OpenWrite(file);
        Generate(stream, autoHeight);
    }

    public abstract void Generate(Stream stream, bool autoHeight = false);
}

public class CardBuilder : ImageBuilder
{
    public required string Title;
    public required IEnumerable<TextRun> Subtitle;
    public required IEnumerable<TextRun> Content;
    public required string IconPlaceholder;

    //public required string TopRightContent;
    public IEnumerable<TextRun>? TopRightContent;
    public required string Footer;
    public byte[]? FooterIcon;
    public required byte[] Icon;

    private static readonly MarkdownPipeline TaskListPipeline = new MarkdownPipelineBuilder()
        .UseTaskLists()
        .Build();

    public static IEnumerable<TextRun> MarkdownRender(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];
        return MarkdownRenderCore(markdown);
    }

    private static IEnumerable<TextRun> MarkdownRenderCore(string markdown)
    {
        string GetRawText(SourceSpan span)
        {
            return markdown[span.Start..span.End];
        }
        var document = Markdown.Parse(markdown, TaskListPipeline);

        TextRun newLine = new(Environment.NewLine) { FontSize = 0 };

        TextRun Indent(int indent, int? fontSize = null, char space = ' ') =>
            new(new string(space, indent)) { FontSize = fontSize };

        IEnumerable<TextRun> InlineToString(ContainerInline? inline, int? fontSize = null)
        {
            if (inline is null)
                yield break;
            foreach (var line in inline)
            {
                switch (line)
                {
                    case LiteralInline literal:
                        yield return new TextRun(literal.Content.ToString(), Color.White)
                        {
                            FontSize = fontSize,
                        };
                        break;
                    case EmphasisInline emphasis:
                        if (emphasis is { DelimiterChar: '`' })
                        {
                            foreach (var run in InlineToString(emphasis, fontSize))
                                yield return run;
                        }
                        else
                        {
                            var isBold =
                                emphasis is { DelimiterChar: not '~', DelimiterCount: 2 or 3 };
                            var isItalic =
                                emphasis is { DelimiterChar: not '~', DelimiterCount: 1 or 3 };
                            var isStrikethrough =
                                emphasis is { DelimiterChar: '~', DelimiterCount: 2 };
                            foreach (var run in InlineToString(emphasis, fontSize))
                                yield return run with
                                {
                                    Bold = isBold,
                                    Italic = isItalic,
                                    Strikethrough = isStrikethrough,
                                };
                        }
                        break;
                    case TaskList task:
                        yield return new TextRun(task.Checked ? "✅ " : "⬜ ") { FontSize = fontSize };
                        break;
                    case LineBreakInline _:
                        yield return new TextRun(Environment.NewLine, Color.White)
                        {
                            FontSize = fontSize,
                        };
                        break;
                    case CodeInline code:
                        yield return new TextRun(code.Content, Color.White)
                        {
                            Italic = true,
                            FontSize = fontSize,
                        };
                        break;
                    case LinkInline link:
                        yield return new TextRun(link.Url ?? GetRawText(link.Span), Color.LightBlue)
                        {
                            Underline = true,
                            FontSize = fontSize,
                        };
                        break;
                    case AutolinkInline autolink:
                        yield return new TextRun(autolink.Url, Color.LightBlue)
                        {
                            Underline = true,
                            FontSize = fontSize,
                        };
                        break;
                    case HtmlInline html:
                        yield return new TextRun(html.Tag, Color.Gray)
                        {
                            Italic = true,
                            FontSize = fontSize,
                        };
                        break;
                    case ContainerInline container:
                        foreach (var run in InlineToString(container, fontSize))
                            yield return run;
                        break;
                    default:
                        yield return new TextRun(GetRawText(line.Span)
#if DEBUG
                                + line.GetType()
#endif
                            , Color.Red)
                        {
                            FontSize = fontSize,
                        };
                        break;
                }
            }
        }

        IEnumerable<TextRun> ProcessBlock(Block block, int level)
        {
            if (level > 10) //防止死循环
            {
                yield return new TextRun("...", Color.Red);
                yield break;
            }
            const int baseFontSize = 15;
            var fontSize = block switch
            {
                HeadingBlock heading => baseFontSize + 8 - heading.Level,
                QuoteBlock or CodeBlock => baseFontSize - 2,
                _ => baseFontSize,
            };

            switch (block)
            {
                case ParagraphBlock paragraph:
                    foreach (var run in InlineToString(paragraph.Inline, fontSize))
                    {
                        yield return run;
                    }

                    break;
                case HeadingBlock heading:
                    foreach (var run in InlineToString(heading.Inline, fontSize))
                    {
                        yield return run;
                    }

                    break;
                case ThematicBreakBlock _:
                    yield return newLine;
                    yield return new TextRun(new string('—', 50), Color.Gray) { FontSize = 8 };
                    break;
                case ListItemBlock listItem:
                    {
                        if (block.Column is > 0 and var col)
                            yield return Indent(col, fontSize);
                    }

                    if (listItem.Order is > 0 and var order)
                    { //带序号
                        yield return new TextRun(order + ". ", Color.Pink) { FontSize = fontSize };
                    }
                    else
                    { //无序号
                        yield return new TextRun(" - ", Color.Pink) { FontSize = fontSize };
                    }
                    foreach (var listItemBlock in listItem)
                    {
                        foreach (var run in ProcessBlock(listItemBlock, level + 1))
                        {
                            yield return run;
                        }
                    }
                    break;
                case ListBlock list:
                    foreach (var item in list)
                    {
                        foreach (var run in ProcessBlock(item, level + 1))
                            yield return run;
                    }
                    break;
                case CodeBlock code:
                    yield return new TextRun(code.Lines.ToString().TrimEnd(), Color.LightSeaGreen)
                    {
                        FontSize = fontSize,
                    };
                    break;
                case QuoteBlock quote:

                    yield return new TextRun(">", Color.LightGoldenrodYellow)
                    {
                        StartPrefix = true,
                    };
                    foreach (var line in quote)
                    {
                        foreach (var run in ProcessBlock(line, level + 1))
                        {
                            yield return run;
                        }
                    }
                    yield return new TextRun(">", Color.LightGoldenrodYellow) { EndPrefix = true };
                    break;
                case HtmlBlock html:
                    var htmlRaw = GetRawText(html.Span);
                    var htmlText = htmlRaw.Length > 20 ? htmlRaw[..20] : htmlRaw;

                    switch (html.Type)
                    {
                        //case HtmlBlockType.DocumentType:
                        //    yield return new TextRun("<!DOCTYPE html>" + htmlText, Color.Gray)
                        //    {
                        //        FontSize = fontSize,
                        //    };
                        //    break;
                        //case HtmlBlockType.CData:
                        //    break;
                        case HtmlBlockType.Comment:
                            //yield return new TextRun("//" + htmlText, Color.Gray)
                            //{
                            //    FontSize = fontSize,
                            //};
                            break;
                        //case HtmlBlockType.ProcessingInstruction:
                        //    break;
                        //case HtmlBlockType.ScriptPreOrStyle:
                        //    break;
                        //case HtmlBlockType.InterruptingBlock:
                        //    break;
                        //case HtmlBlockType.NonInterruptingBlock:
                        //    break;
                        default:
                            yield return new TextRun(htmlText, Color.LightSeaGreen)
                            {
                                FontSize = fontSize,
                            };
                            break;
                    }
                    break;
                default:
                    yield return new TextRun(GetRawText(block.Span)
#if DEBUG
                            + block.GetType()
#endif
                        , Color.Red);
                    break;
            }

            yield return newLine;
        }
        foreach (var block in document)
        {
            foreach (var run in ProcessBlock(block, 0))
            {
                yield return run;
            }
        }
    }

    public static byte[] CharToImage(char text, Font font, Color color, int? size = null)
    {
        var sizeInt = (size ?? (int)font.Size) * Scale;
        return TextToImage(text.ToString(), new Font(font, font.Size * Scale), color, sizeInt, sizeInt);
    }

    public static byte[] TextToImage(string text, Font font, Color color, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var textGraphicOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Origin = new Vector2(width / 2f, height / 2f),
        };
        image.Mutate(ctx => ctx.DrawText(textGraphicOptions, text, new SolidBrush(color)));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Renders the card into <paramref name="stream"/> as PNG. Layout is in logical pixels and
    /// drawn at <see cref="Scale"/>x so it stays sharp on high-density screens.
    /// </summary>
    /// <param name="autoHeight">Grow the card to fit the content, up to <see cref="MaxHeight"/>.</param>
    public override void Generate(Stream stream, bool autoHeight = false)
    {
        const int width = 500;
        const int iconWidth = 60;
        const int contentTop = 68;
        var font = FontManager.ComicSansMs.CreateFont(20);
        var chineseFont = FontManager.MaoKenTangYuan.CreateFont(20);
        var emojiFont = FontManager.TwEmoji.CreateFont(20);
        var fallbackFontFamilies = new List<FontFamily> { chineseFont.Family, emojiFont.Family };
        var background = Color.Black;
        var secondaryBrush = new SolidBrush(Color.FromRgb(167, 169, 181));

        var (contentText, contentRuns) = BuildTextRuns(Content);
        var contentOptions = new RichTextOptions(font)
        {
            WrappingLength = ContentWidth,
            Origin = new PointF(iconWidth, contentTop),
            FallbackFontFamilies = fallbackFontFamilies,
            LineSpacing = 1.1f,
            TextRuns = contentRuns,
            ColorFontSupport = ColorFontSupport.MicrosoftColrFormat,
        };
        float contentHeight = MaxHeight;
        try
        {
            contentHeight = TextMeasurer.MeasureSize(contentText, contentOptions).Height;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        var height = autoHeight
            ? Math.Clamp((int)contentHeight + contentTop + FooterHeight + 8, MinHeight, MaxHeight)
            : MinHeight;
        var contentBottom = height - FooterHeight;

        using var image = new Image<Rgba32>(width * Scale, height * Scale);
        using var icon = Image.Load(Icon);
        icon.Mutate(x => x.Resize(30 * Scale, 30 * Scale, KnownResamplers.Bicubic));
        using var footerIcon = FooterIcon is not null ? Image.Load(FooterIcon) : null;
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(background)
                .DrawImage(icon, new Point(15 * Scale, 15 * Scale), 1)
                .SetDrawingTransform(Matrix3x2.CreateScale(Scale));
            var topRight = TopRightContent is null ? default : BuildTextRuns(TopRightContent);
            var topRightOptions = new RichTextOptions(font)
            {
                Origin = new Vector2(width - 10, 10),
                HorizontalAlignment = HorizontalAlignment.Right,
                FallbackFontFamilies = fallbackFontFamilies,
                TextRuns = topRight.runs ?? [],
            };
            var topRightWidth = topRight.text is { Length: > 0 }
                ? TextMeasurer.MeasureSize(topRight.text, topRightOptions).Width + 12
                : 0;
            ctx.DrawText(
                new RichTextOptions(chineseFont)
                {
                    Origin = new Vector2(iconWidth, 10),
                    FallbackFontFamilies = fallbackFontFamilies,
                },
                Fit(Title, chineseFont, fallbackFontFamilies, width - iconWidth - 10 - topRightWidth),
                secondaryBrush
            );
            if (topRight.text is { Length: > 0 })
                ctx.DrawText(topRightOptions, topRight.text, secondaryBrush);
            {
                var (text, runs) = BuildTextRuns(FitRuns(Subtitle, font, fallbackFontFamilies, width - iconWidth - 10));
                ctx.DrawText(
                    new RichTextOptions(font)
                    {
                        Origin = new Vector2(iconWidth, 45),
                        VerticalAlignment = VerticalAlignment.Center,
                        FallbackFontFamilies = fallbackFontFamilies,
                        TextRuns = runs,
                    },
                    text,
                    secondaryBrush
                );
            }
            if (!string.IsNullOrEmpty(IconPlaceholder))
                ctx.DrawText(
                    new RichTextOptions(new Font(font.Family, 10))
                    {
                        Origin = new Vector2(30, 50),
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                    IconPlaceholder,
                    secondaryBrush
                );
            ctx.DrawText(contentOptions, contentText, new SolidBrush(Color.White));
            if (contentTop + contentHeight > contentBottom)
            {
                // Fade out content that does not fit and hide it behind the footer.
                ctx.Fill(
                    new LinearGradientBrush(
                        new PointF(0, contentBottom - 36),
                        new PointF(0, contentBottom),
                        GradientRepetitionMode.None,
                        new ColorStop(0, Color.Transparent),
                        new ColorStop(1, background)
                    ),
                    new RectangleF(0, contentBottom - 36, width, 36)
                );
                ctx.Fill(background, new RectangleF(0, contentBottom, width, height - contentBottom));
            }
            var footerX = iconWidth;
            if (footerIcon is not null)
            {
                footerIcon.Mutate(x =>
                {
                    ApplyRoundedCorners(x, footerIcon.Width / 2f);
                    x.Resize(24 * Scale, 24 * Scale, KnownResamplers.Bicubic);
                });
                ctx.DrawImage(footerIcon, new Point(iconWidth * Scale, (height - 38) * Scale), 1);
                footerX += 32;
            }
            ctx.DrawText(
                new RichTextOptions(chineseFont)
                {
                    Origin = new Vector2(footerX, height - 26),
                    VerticalAlignment = VerticalAlignment.Center,
                    FallbackFontFamilies = fallbackFontFamilies,
                },
                Fit(Footer, chineseFont, fallbackFontFamilies, width - footerX - 10),
                secondaryBrush
            );
            ctx.SetDrawingTransform(Matrix3x2.Identity);
            ApplyRoundedCorners(ctx, 15 * Scale);
        });
        image.SaveAsPng(stream);
    }

    public const int Scale = 2;

    /// <summary>Width available to <see cref="Content"/>, in logical pixels.</summary>
    public const int ContentWidth = 420;
    private const int FooterHeight = 52;
    private const int MinHeight = 200;
    private const int MaxHeight = 600;

    /// <summary>Truncates <paramref name="runs"/> so they fit on one line of <see cref="Content"/>.</summary>
    public static IReadOnlyList<TextRun> FitContentLine(IEnumerable<TextRun> runs) =>
        FitRuns(
            runs,
            FontManager.ComicSansMs.CreateFont(20),
            [FontManager.MaoKenTangYuan.CreateFont(20).Family, FontManager.TwEmoji.CreateFont(20).Family],
            ContentWidth
        );

    private static string Fit(string text, Font font, List<FontFamily> fallback, float maxWidth)
    {
        var options = new TextOptions(font) { FallbackFontFamilies = fallback };
        if (TextMeasurer.MeasureSize(text, options).Width <= maxWidth)
            return text;
        var graphemes = Graphemes(text);
        for (var n = graphemes.Count - 1; n > 0; n--)
        {
            var candidate = string.Concat(graphemes.Take(n)).TrimEnd() + "…";
            if (TextMeasurer.MeasureSize(candidate, options).Width <= maxWidth)
                return candidate;
        }
        return "…";
    }

    /// <summary>Drops trailing text from <paramref name="runs"/> until one line fits.</summary>
    private static List<TextRun> FitRuns(
        IEnumerable<TextRun> runs,
        Font font,
        List<FontFamily> fallback,
        float maxWidth
    )
    {
        var list = runs.ToList();
        while (true)
        {
            var built = BuildTextRuns(list);
            var options = new RichTextOptions(font) { FallbackFontFamilies = fallback, TextRuns = built.runs };
            if (list.Count == 0 || TextMeasurer.MeasureSize(built.text, options).Width <= maxWidth)
                return list;
            var last = list[^1];
            var graphemes = Graphemes(last.Text.TrimEnd('…'));
            list[^1] = graphemes.Count <= 1
                ? last with { Text = "" }
                : last with { Text = string.Concat(graphemes.Take(graphemes.Count - 1)).TrimEnd() + "…" };
            if (list[^1].Text.Length == 0)
                list.RemoveAt(list.Count - 1);
        }
    }

    private static List<string> Graphemes(string text)
    {
        var result = new List<string>();
        var enumerator = new SpanGraphemeEnumerator(text);
        while (enumerator.MoveNext())
            result.Add(enumerator.Current.ToString());
        return result;
    }
}
