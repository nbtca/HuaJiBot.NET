using System.Numerics;
using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Path = System.IO.Path;

namespace HuaJiBot.NET.Utils;

public record TextRun(string Text, Color Color)
{
    public TextRun(string text)
        : this(text, Color.White) { }

    public static implicit operator TextRun(string text) => new(text, Color.White);
}

public class CardBuilder
{
    public required string Title;
    public required IEnumerable<TextRun> Content;
    public required string IconPlaceholder;

    //public required string TopRightContent;
    public IEnumerable<TextRun>? TopRightContent;
    public required string Footer;
    public byte[]? FooterIcon;
    public required byte[] Icon;

    #region ProcessAvatarWithRoundedCorner
    //ref from https://github.com/SixLabors/Samples/blob/main/ImageSharp/AvatarWithRoundedCorner/Program.cs#L52
    // This method can be seen as an inline implementation of an `IImageProcessor`:
    // (The combination of `IImageOperations.Apply()` + this could be replaced with an `IImageProcessor`)
    private static IImageProcessingContext ApplyRoundedCorners(
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
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
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

    public class AutoDeleteFile(string file) : IDisposable
    {
        public void Dispose()
        { //delete after 30s
            Task.Delay(30_000)
                .ContinueWith(_ =>
                {
                    File.Delete(file);
                });
        }

        public static implicit operator string(AutoDeleteFile file) => file.FileName;

        public string FileName => file;
    }

    public AutoDeleteFile SaveTempAutoDelete()
    {
        var tempName = Path.GetTempFileName();
        var tempDir = Path.Combine(Environment.CurrentDirectory, "temp");
        if (!Directory.Exists(tempDir))
            Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, tempName);
        Save(tempFile);
        return new AutoDeleteFile(tempFile);
    }

    public void Save(string file)
    {
        using var stream = File.OpenWrite(file);
        Save(stream);
    }

    public void Save(Stream stream)
    {
        const int width = 500;
        const int height = 200;
        using var image = new Image<Rgba32>(width, height);
        // 选择字体、颜色和布局
        var font = SystemFonts.CreateFont("Comic Sans MS", 20);
        var yaHeiFont = SystemFonts.CreateFont("Microsoft YaHei", 20);
        var background = Color.Black;
        const int iconWidth = 60;
        var secondaryBrush = new SolidBrush(Color.FromRgb(167, 169, 181));
        using var iconStream = new MemoryStream(Icon);
        //加载图标
        var icon = Image.Load(iconStream);
        icon.Mutate(x => x.Resize(30, 30, KnownResamplers.Bicubic).Invert());
        var footerIcon = FooterIcon is not null ? Image.Load(new MemoryStream(FooterIcon)) : null;
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(background)
                // 绘制图标
                .DrawImage(icon, new Point(15, 15), 1)
                // 绘制标题文本
                .DrawText(Title, yaHeiFont, secondaryBrush, new PointF(iconWidth, 10))
                //图标下方的文字
                .DrawText(
                    new RichTextOptions(new Font(font.Family, 10))
                    {
                        Origin = new Vector2(30, 45),
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                    IconPlaceholder,
                    secondaryBrush
                );
            #region Build
            (string text, IReadOnlyList<RichTextRun> runs) BuildTextRuns(IEnumerable<TextRun> runs)
            {
                var sb = new StringBuilder();
                var list = new List<RichTextRun>();
                foreach (var (text, color) in runs)
                {
                    list.Add(
                        new RichTextRun
                        {
                            Brush = new SolidBrush(color),
                            Start = sb.Length,
                            End = sb.Length + text.Length
                        }
                    );
                    sb.Append(text);
                }
                return (sb.ToString(), list);
            }
            #endregion
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
                            FallbackFontFamilies = new[] { yaHeiFont.Family },
                            TextRuns = runs
                        },
                        text,
                        secondaryBrush
                    );
                }
            }
            // 绘制内容
            {
                var (text, runs) = BuildTextRuns(Content);
                ctx.DrawText(
                    new RichTextOptions(font)
                    {
                        WrappingLength = width - iconWidth - 20,
                        Origin = new PointF(iconWidth, 40),
                        FallbackFontFamilies = new[] { yaHeiFont.Family },
                        LineSpacing = 1.1f,
                        TextRuns = runs
                    },
                    text,
                    new SolidBrush(Color.White)
                );
            }
            //TextMeasurer.MeasureSize(options)//测量文本大小
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
                ctx.DrawText(Footer, yaHeiFont, secondaryBrush, new PointF(iconWidth + 30, 160))
                    .DrawImage(footerIcon, new Point(iconWidth, 159), 1);
            }
            else
            {
                ctx.DrawText(Footer, yaHeiFont, secondaryBrush, new PointF(iconWidth, 160));
            }

            ApplyRoundedCorners(ctx, 15);
        });
        image.SaveAsPng(stream);
    }
}
