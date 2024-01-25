using System.Diagnostics;
using HuaJiBot.NET.Plugin.Calendar;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using Path = System.IO.Path;

namespace HuaJiBot.NET.UnitTest;

internal class BuildImageTest
{
    [SetUp]
    public void Setup() { }

    //[Test]
    //public void TestSvg()
    //{
    //    using var s = new MemoryStream(HuaJiBot.NET.Utils.CardBuilder);
    //    Assert.Pass(Image.DetectFormat(s).ToString());
    //    Image.Load(s);
    //}

    #region Images

    private static byte[] _commitIcon = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAN5JREFUSEvt0zFqAkEUh/HfksPY2FvEwnQiBok38ACSJthqadqkSxUQayFgYWcleoAUOUWOkLCygiy7O4Zlqjjlzs7/m/e9eYnIK4mc7woIGv4/itqYo4EffOENi5CjSxTNMC0JGuG9ChIC9LDOAg54xA2e8JB9H+CjDBICpAfv8YlmLuQFY2xxdwkgdVu2utjkNlvYV5w5Xv68girALXZ1AUWXOSlaYZj74RmTvygqApw3+RVLfGd96aODWk1OoVGf6amqqIMWGtbK/dAc1ArPP9PaYUUB1wqCWqMr+gV7eiMZdZi41AAAAABJRU5ErkJggg=="
    );

    #endregion
    [Test]
    public async Task Build()
    {
        var avatarUrl = "https://avatars.githubusercontent.com/u/91080742?v=4";
        var avatar = await AvatarHelper.Get(avatarUrl);

        CardBuilder card =
            new()
            {
                Title = "nbtca / blogs : article",
                Subtitle = new Func<IEnumerable<TextRun>>(() =>
                {
                    var font = IconFonts.IcoMoonFont(19);
                    var lang = "Go";
                    var starCount = "1";
                    var forksCount = "2";
                    var pullsCount = "3";
                    var subtitleColor = Color.FromRgb(139, 148, 158);
                    var list = new List<TextRun>
                    {
                        new(
                            IconFonts.IconCircle,
                            LangColorsHelper.GetColor(lang, out var color)
                            && color is { r: var r, g: var g, b: var b }
                                ? Color.FromRgb(r, g, b)
                                : subtitleColor,
                            font
                        ),
                        " ",
                        new(lang, subtitleColor)
                    };
                    void add(char icon, string text)
                    {
                        list.Add("  ");
                        list.Add(new(icon, subtitleColor, font));
                        list.Add(" ");
                        list.Add(new(text, subtitleColor));
                    }
                    add(IconFonts.IconStars, starCount);
                    add(IconFonts.IconForks, forksCount);
                    add(IconFonts.IconPulls, pullsCount);
                    add(IconFonts.IconLaw, "MIT");

                    return list;
                }).Invoke(),
                Content = new TextRun[] { "迁移文章 by ", "测试", "test", "s" },
                Footer = "@nbtca pushed 1 commit.",
                FooterIcon = avatar,
                Icon = _commitIcon,
                IconPlaceholder = "fb6edcb1\nfb6edcb1\nfb6edcb1",
                TopRightContent = new[]
                {
                    new TextRun(" +2", Color.ParseHex("#2cbe4e")),
                    new TextRun(" -3", Color.ParseHex("#eb2431")),
                    new TextRun(" ~4", Color.ParseHex("#ffc000")),
                }
            };
        // 保存图像到文件
        var file = Path.GetFullPath("output.png");
        Console.WriteLine(file);
        card.Generate(file);
        Process.Start("cmd", ["/C", file]);
        Console.WriteLine("图片已生成。");
    }

    [Test]
    public async Task BuildCalendar()
    {
        //// 保存图像到文件
        //var file = Path.GetFullPath("output2.png");
        //Console.WriteLine(file);
        //card.Generate(file);
        //Process.Start("cmd", ["/C", file]);
        //Console.WriteLine("图片已生成。");
    }
}
