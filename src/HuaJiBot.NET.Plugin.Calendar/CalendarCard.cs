using HuaJiBot.NET.Utils;
using HuaJiBot.NET.Utils.Fonts;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TextRun = HuaJiBot.NET.Utils.TextRun;

namespace HuaJiBot.NET.Plugin.Calendar;

public class CalendarCard(string start) : ImageBuilder
{
    public override void Generate(Stream stream)
    {
        var width = 500;
        var height = 200;
        using var image = new Image<Rgba32>(width, height);
        //导入字体
        //new FontCollection()    .
        var font = FontManager.ComicMono.CreateFont(20);
        var yaHeiFont = FontManager.MaoKenTangYuan.CreateFont(20);
        //生成文本
        var (text, runs) = BuildTextRuns(
            new TextRun[] { new("测试"), new("test", Color.ParseHex("#2cbe4e")), }
        );

        image.Mutate(ctx =>
        {
            var size = ctx.GetCurrentSize();
            var background = Color.Black;
            //设置背景色
            ctx.BackgroundColor(background);
            #region 内容
            //设置文本选项
            var options = new RichTextOptions(
                font /*主字体*/
            )
            {
                TextRuns = runs,
                FallbackFontFamilies = new[] { yaHeiFont.Family } //设置回落字体，
            };
            //测量文本大小，以确定最终的布局
            var rectangle = TextMeasurer.MeasureSize(text, options);
            var y = rectangle.Height + rectangle.Location.Y;
            Console.WriteLine(JsonConvert.SerializeObject(rectangle));
            //根据测量的文本高度调整画布大小
            ctx.Resize(
                new ResizeOptions
                {
                    Size = new Size(size.Width, (int)y + 10), //+10是为了留出一点空隙
                    CenterCoordinates = new PointF(0, 0),
                    Mode = ResizeMode.Pad,
                    Position = AnchorPositionMode.Top,
                    PadColor = background
                }
            );
            //绘制文本
            ctx.DrawText(options, text, new SolidBrush(Color.FromRgb(167, 169, 181)));
            #endregion
        });
        image.SaveAsPng(stream);
    }
}
