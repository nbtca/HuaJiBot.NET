using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using HuaJiBot.NET.Utils.Fonts;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SixLabors.Fonts;
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
    /// <summary>
    /// 将富文本转换为ImageSharp的RichTextRun
    /// </summary>
    /// <param name="runs"></param>
    /// <returns></returns>
    protected (string text, IReadOnlyList<RichTextRun> runs) BuildTextRuns(
        IEnumerable<TextRun> runs
    )
    {
        var sb = new StringBuilder();
        var list = new List<RichTextRun>();

        var count = 0;
        var defaultFont = new Lazy<Font>(() => FontManager.MaoKenTangYuan.CreateFont(20));
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
                Start = sb.Length,
                End = sb.Length + text.Length,
                Font = font,
                TextAttributes = line.TextAttributes,
            };
            if (line.Underline)
                run.TextDecorations = TextDecorations.Underline;
            //if (line.Italic || line.Bold)
            //{
            //var boldStrokeWidth = .5f;
            //if (line.Italic)
            //{
            //    float[] pattern = [3, 2, 1];
            //    run.Pen = line.Bold
            //        ? new PatternPen(color, boldStrokeWidth, pattern)
            //        : new PatternPen(color, pattern);
            //}
            //else if (line.Bold)
            //{
            //    run.Pen = new SolidPen(color, boldStrokeWidth);
            //}
            //}
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
    /// 用于出作用域自动删除文件
    /// </summary>
    public class AutoDeleteFile : IDisposable
    {
        private readonly string _file;

        internal AutoDeleteFile(string file)
        {
            _file = file;
        }

        public void Dispose() //销毁
        { //delete after 30s
            Task.Delay(30_000)
                .ContinueWith(_ =>
                {
                    File.Delete(_file); //删除文件
                });
        }

        public static implicit operator string(AutoDeleteFile file) => file.FileName;

        public string FileName => _file;
    }

    /// <summary>
    /// 保存到临时文件
    /// </summary>
    /// <returns>自动删除文件</returns>
    public AutoDeleteFile SaveTempAutoDelete(bool autoHeight = false)
    {
        var tempName = Path.GetTempFileName();
        var tempDir = Path.Combine(Environment.CurrentDirectory, "temp");
        if (!Directory.Exists(tempDir))
            Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, tempName);
        Generate(tempFile, autoHeight);
        return new AutoDeleteFile(tempFile);
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

    public static IEnumerable<TextRun> MarkdownRender(string markdown)
    {
        string GetRawText(SourceSpan span)
        {
            return markdown[span.Start..span.End];
        }
        var document = Markdown.Parse(markdown);

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
                    yield return new TextRun(GetRawText(code.Span), Color.LightSeaGreen)
                    {
                        Italic = true,
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
        var sizeInt = size ?? (int)font.Size;
        return TextToImage(text.ToString(), font, color, sizeInt, sizeInt);
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
    /// 生成图像
    /// 并输出到 <see cref="Stream"/> 中
    /// </summary>
    /// <param name="stream">输出流</param>
    /// <param name="autoHeight">自动测量高度</param>
    public override void Generate(Stream stream, bool autoHeight = false)
    {
        const int width = 500;
        int height = 200;
        using var image = new Image<Rgba32>(width, height);
        // 选择字体、颜色和布局
        var font = FontManager.ComicSansMs.CreateFont(20);
        var yaHeiFont = FontManager.MaoKenTangYuan.CreateFont(20);
        var background = Color.Black;
        const int iconWidth = 60;
        var secondaryBrush = new SolidBrush(Color.FromRgb(167, 169, 181));
        using var iconStream = new MemoryStream(Icon);
        //加载图标
        var icon = Image.Load(iconStream);
        icon.Mutate(x => x.Resize(30, 30, KnownResamplers.Bicubic));
        var footerIcon = FooterIcon is not null ? Image.Load(new MemoryStream(FooterIcon)) : null;
        image.Mutate(ctx => //变换
        {
            ctx.BackgroundColor(background)
                // 绘制图标
                .DrawImage(icon, new Point(15, 15), 1)
                // 绘制标题文本
                .DrawText(Title, yaHeiFont, secondaryBrush, new PointF(iconWidth, 10));
            // 绘制副标题文本
            {
                var (text, runs) = BuildTextRuns(Subtitle);
                ctx.DrawText(
                    new RichTextOptions(font)
                    {
                        Origin = new Vector2(iconWidth, 45),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        FallbackFontFamilies = [yaHeiFont.Family],
                        TextRuns = runs,
                    },
                    text,
                    secondaryBrush
                );
            }
            //图标下方的文字
            ctx.DrawText(
                new RichTextOptions(new Font(font.Family, 10))
                {
                    Origin = new Vector2(30, 45),
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
                IconPlaceholder,
                secondaryBrush
            );
            //右上角的内容
            {
                if (TopRightContent is not null)
                {
                    var (text, runs) = BuildTextRuns(TopRightContent);
                    ctx.DrawText(
                        new RichTextOptions(font)
                        {
                            Origin = new Vector2(width - 10, 10),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            FallbackFontFamilies = [yaHeiFont.Family],
                            TextRuns = runs,
                        },
                        text,
                        secondaryBrush
                    );
                }
            }
            // 绘制主体内容
            {
                var (text, runs) = BuildTextRuns(Content);
                var richTextOptions = new RichTextOptions(font)
                {
                    WrappingLength = width - iconWidth - 20,
                    Origin = new PointF(iconWidth, 60),
                    FallbackFontFamilies = [yaHeiFont.Family],
                    LineSpacing = 1.1f,
                    TextRuns = runs,
                };
                if (autoHeight)
                {
                    var measureSize = TextMeasurer.MeasureSize(text, richTextOptions); //测量文本大小
                    height = (int)measureSize.Height + 130; //设置高度
                    ctx.Resize(
                        new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Position = AnchorPositionMode.Top,
                            Mode = ResizeMode.Pad,
                            PadColor = background,
                        }
                    ); //调整大小
                }
                ctx.DrawText(richTextOptions, text, new SolidBrush(Color.White));
            }
            // 淡化下方超出范围的文本内容
            ctx.Fill(
                new LinearGradientBrush( //Gradient from opaque to background color
                    new PointF(0, height - 90), //起点
                    new PointF(0, height - 32), //终点
                    GradientRepetitionMode.None,
                    new ColorStop(0, Color.Transparent), //透明
                    new ColorStop(1, background) //背景
                ),
                new RectangleF(0, 100, width, 100)
            );
            // 绘制底部文本
            if (footerIcon is not null)
            {
                //头像
                footerIcon.Mutate(x =>
                {
                    ApplyRoundedCorners(x, footerIcon.Width / 2f); //圆角
                    x.Resize(25, 25, KnownResamplers.Bicubic); //缩放
                });
                ctx.DrawText(
                        Footer,
                        yaHeiFont,
                        secondaryBrush,
                        new PointF(iconWidth + 30, height - (200 - 160))
                    )
                    .DrawImage(footerIcon, new Point(iconWidth, height - (200 - 158)), 1);
            }
            else
            {
                ctx.DrawText(
                    Footer,
                    yaHeiFont,
                    secondaryBrush,
                    new PointF(iconWidth, height - (200 - 160))
                );
            }

            ApplyRoundedCorners(ctx, 15);
        });
        image.SaveAsPng(stream);
    }
}
