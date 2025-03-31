using System.Diagnostics;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using HuaJiBot.NET.Utils.Fonts;
using SixLabors.ImageSharp;
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


    private string avatarUrl = "https://avatars.githubusercontent.com/u/91080742?v=4";

    [Test]
    public async Task GitIssue()
    {
        var text = "test";
        text =
            "你好\ud83d\ude0a"
            + "你好\ud83d\udc69\u200d\ud83d\udc69\u200d\ud83d\udc67\u200d\ud83d\udc66" // 家庭 emoji，是一个连接的 emoji
            + "你好\ud83d\udc4d"
            + "你好\ud83d\udc68\u200d\u2695\ufe0f" // 职业 emoji，也是一个连接的 emoji
            + "\ud83d\ude00\ud83d\ude03\ud83d\ude04\ud83d\ude01\ud83d\ude06\ud83d\ude05\ud83d\ude02\ud83d\ude03\ud83d\ude04\ud83d\ude01\ud83d\ude06\ud83d\ude05\ud83d\ude02\ud83e\udd23\r\n\ud83d\ude03\ud83d\ude04\ud83d\ude01\ud83d\ude06\ud83d\ude05\ud83d\ude02\ud83e\udd23 \ud83d\ude0a\ud83d\ude07\ud83d\ude42\ud83d\ude43\ud83d\ude09\ud83d\ude0c\ud83d\ude0d\ud83e\udd70\ud83d\ude18\ud83d\ude17\ud83d\ude19\ud83d\ude1a\ud83d\ude0b\ud83d\ude1b\ud83d\ude1d\ud83d\ude1c\ud83e\udd2a\ud83e\udd28\ud83e\uddd0\ud83e\udd13\ud83d\ude0e\ud83e\udd78\ud83e\udd29\ud83e\udd73\ud83d\ude42\u200d\u2195\ufe0f\ud83d\ude0f\ud83d\ude12\ud83d\ude42\u200d\u2194\ufe0f\ud83d\ude1e\ud83d\ude14\ud83d\ude1f\ud83d\ude15\ud83d\ude41\u2639\ufe0f\ud83d\ude23\ud83d\ude16\ud83d\ude2b\ud83d\ude29\ud83e\udd7a\ud83d\ude22\ud83d\ude2d\ud83d\ude2e\u200d\ud83d\udca8\ud83d\ude24\ud83d\ude20\ud83d\ude21\ud83e\udd2c\ud83e\udd2f\ud83d\ude33\ud83e\udd75\ud83e\udd76\ud83d\ude31\ud83d\ude28\ud83d\ude30\ud83d\ude25\ud83d\ude13\ud83e\udee3\ud83e\udd17\ud83e\udee1\ud83e\udd14\ud83e\udee2\ud83e\udd2d\ud83e\udd2b\ud83e\udd25\ud83d\ude36\ud83d\ude36\u200d\ud83c\udf2b\ufe0f\ud83d\ude10\ud83d\ude11\ud83d\ude2c\ud83e\udee8\ud83e\udee0\ud83d\ude44\ud83d\ude2f\ud83d\ude26\ud83d\ude27\ud83d\ude2e\ud83d\ude32\ud83e\udd71\ud83d\ude34\ud83e\udee9\ud83e\udd24\ud83d\ude2a\ud83d\ude35\ud83d\ude35\u200d\ud83d\udcab\ud83e\udee5\ud83e\udd10\ud83e\udd74\ud83e\udd22\ud83e\udd2e\ud83e\udd27\ud83d\ude37\ud83e\udd12\ufffd\ufffd\ud83e\udd11\ud83e\udd20\ud83d\ude08\ud83d\udc7f\ud83d\udc79\ud83d\udc7a\ud83e\udd21\ud83d\udca9";

        //
        //

        //text = """
        //    # Markdown Syntax Guide

        //    ## Headings

        //    # H1 Heading
        //    ## H2 Heading
        //    ### H3 Heading
        //    #### H4 Heading
        //    ##### H5 Heading
        //    ###### H6 Heading

        //    ## Text Formatting

        //    **Bold Text**
        //    *Italic Text*
        //    ***Bold and Italic***
        //    ~~Strikethrough~~

        //    ## Blockquotes

        //    > This is a blockquote.
        //    >
        //    > - Nested item 1
        //    >   - Nested subitem 1
        //    > - Nested item 2


        //    ## Lists

        //    ### Unordered List

        //    - Item 1
        //      - Subitem 1
        //      - Subitem 2
        //    - Item 2

        //    ### Ordered List

        //    1. First item
        //    2. Second item
        //       1. Subitem 1
        //       2. Subitem 2
        //    3. Third item

        //    ## Code

        //    ### Inline Code

        //    Use `inline code` for small snippets.

        //    ### Code Blocks

        //    ```
        //    # Python code example
        //    def greet(name):
        //        return f"Hello, {name}!"
        //    ```


        //    ## Horizontal Rules

        //    ---

        //    ## Links

        //    [Markdown Guide](https://www.markdownguide.org)

        //    ### Image

        //    ![Markdown Logo](https://upload.wikimedia.org/wikipedia/commons/4/48/Markdown-mark.svg)

        //    ## Tables

        //    | Syntax      | Description |
        //    |-------------|-------------|
        //    | Header      | Title       |
        //    | Paragraph   | Text        |

        //    ## Task Lists

        //    - [x] Task 1
        //    - [ ] Task 2
        //    - [ ] Task 3
        //        - [ ] Task 2
        //        - [ ] Task 3

        //    ## Inline HTML

        //    <div style="color: red;">This is a red text block in HTML.</div>

        //    ## Escape Characters

        //    \*Literal asterisks\*

        //    ## Footnotes

        //    Here is a footnote reference[^1].

        //    [^1]: This is the footnote content.

        //    ## Math (if supported by parser)

        //    Inline math: $a^2 + b^2 = c^2$

        //    Block math:

        //    $$
        //    \int_0^\infty e^{-x^2} dx = \frac{\sqrt{\pi}}{2}
        //    $$

        //    """;
        var avatar = await AvatarHelper.GetAsync(avatarUrl);
        //var icon = CardBuilder.CharToImage(
        //    IconFonts.IssueOpened,
        //    IconFonts.IcoMoonFont(25),
        //    Color.FromRgb(248, 81, 73),
        //    30
        //); //open
        //var icon = CardBuilder.CharToImage(
        //    IconFonts.ActionSkip,
        //    IconFonts.IcoMoonFont(25),
        //    Color.FromRgb(145, 152, 161),
        //    30
        //); //not plan
        var icon = CardBuilder.CharToImage(
            IconFonts.IssueClosed,
            IconFonts.IcoMoonFont(25),
            Color.FromRgb(171, 125, 248),
            30
        ); //done
        CardBuilder card = new()
        {
            Title = "nbtca / blogs",
            Subtitle = new Func<IEnumerable<TextRun>>(() =>
            {
                var lang = "Go";
                var titleColor = Color.Azure;
                var gray = Color.FromRgb(139, 148, 158);
                var langColor =
                    LangColorsHelper.GetColor(lang, out var color)
                    && color is { r: var r, g: var g, b: var b }
                        ? Color.FromRgb(r, g, b)
                        : gray;
                return new List<TextRun>
                {
                    new("# ", langColor),
                    new("test11222222222222", titleColor),
                    new("  by:", gray),
                    new("xxx", gray),
                };
            }).Invoke(),
            Content = CardBuilder.MarkdownRender(text),
            Footer = "@nbtca create issue",
            FooterIcon = avatar,
            Icon = icon,
            IconPlaceholder = "",
            TopRightContent =
            [
                //new TextRun(" +2", Color.ParseHex("#2cbe4e")),
                //new TextRun(" -3", Color.ParseHex("#eb2431")),
                //new TextRun(" ~4", Color.ParseHex("#ffc000")),
            ],
        };
        // 保存图像到文件
        var file = Path.GetFullPath("output.png");
        Console.WriteLine(file);
        card.Generate(file, true);
        Process.Start("cmd", ["/C", file]);
        Console.WriteLine("图片已生成。");
    }

    [Test]
    public async Task GitPush()
    {
        var avatar = await AvatarHelper.GetAsync(avatarUrl);

        CardBuilder card = new()
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
                    new(lang, subtitleColor),
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
            Content = ["迁移文章 by ", "测试", "test", "s"],
            Footer = "@nbtca pushed 1 commit.",
            FooterIcon = avatar,
            Icon = CardBuilder.CharToImage(
                IconFonts.IconCommit,
                IconFonts.IcoMoonFont(30),
                Color.White
            ),
            IconPlaceholder = "fb6edcb1\nfb6edcb1\nfb6edcb1",
            TopRightContent =
            [
                new TextRun(" +2", Color.ParseHex("#2cbe4e")),
                new TextRun(" -3", Color.ParseHex("#eb2431")),
                new TextRun(" ~4", Color.ParseHex("#ffc000")),
            ],
        };
        // 保存图像到文件
        var file = Path.GetFullPath("output.png");
        Console.WriteLine(file);
        card.Generate(file);
        Process.Start("cmd", ["/C", file]);
        Console.WriteLine("图片已生成。");
    }

    [Test]
    public Task BuildTextImage()
    {
        var file = Path.GetFullPath("output.png");
        Console.WriteLine(file);
        var bytes = CardBuilder.CharToImage(
            IconFonts.IconCommit,
            IconFonts.IcoMoonFont(30),
            Color.Red
        );
        File.WriteAllBytes(file, bytes);
        Process.Start("cmd", ["/C", file]);
        Console.WriteLine("图片已生成。");
        return Task.CompletedTask;
    }

    [Test]
    public Task BuildCalendar()
    {
        return Task.CompletedTask;
        //// 保存图像到文件
        //var file = Path.GetFullPath("output2.png");
        //Console.WriteLine(file);
        //card.Generate(file);
        //Process.Start("cmd", ["/C", file]);
        //Console.WriteLine("图片已生成。");
    }
}
