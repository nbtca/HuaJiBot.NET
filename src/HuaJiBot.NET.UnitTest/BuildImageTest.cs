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
        CardBuilder card =
            new()
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
                Content = CardBuilder.MarkdownRender(
                    """
                    # Markdown Syntax Guide

                    ## Headings

                    # H1 Heading
                    ## H2 Heading
                    ### H3 Heading
                    #### H4 Heading
                    ##### H5 Heading
                    ###### H6 Heading

                    ## Text Formatting

                    **Bold Text**
                    *Italic Text*
                    ***Bold and Italic***
                    ~~Strikethrough~~

                    ## Blockquotes

                    > This is a blockquote.
                    >
                    > - Nested item 1
                    >   - Nested subitem 1
                    > - Nested item 2
                        

                    ## Lists

                    ### Unordered List

                    - Item 1
                      - Subitem 1
                      - Subitem 2
                    - Item 2

                    ### Ordered List

                    1. First item
                    2. Second item
                       1. Subitem 1
                       2. Subitem 2
                    3. Third item

                    ## Code

                    ### Inline Code

                    Use `inline code` for small snippets.

                    ### Code Blocks

                    ```
                    # Python code example
                    def greet(name):
                        return f"Hello, {name}!"
                    ```

                        ```javascript
                        // JavaScript example
                        function greet(name) {
                            return `Hello, ${name}!`;
                        }
                        ```

                    ## Horizontal Rules

                    ---

                    ## Links

                    [Markdown Guide](https://www.markdownguide.org)

                    ### Image

                    ![Markdown Logo](https://upload.wikimedia.org/wikipedia/commons/4/48/Markdown-mark.svg)

                    ## Tables

                    | Syntax      | Description |
                    |-------------|-------------|
                    | Header      | Title       |
                    | Paragraph   | Text        |

                    ## Task Lists

                    - [x] Task 1
                    - [ ] Task 2
                    - [ ] Task 3
                        - [ ] Task 2
                        - [ ] Task 3

                    ## Inline HTML

                    <div style="color: red;">This is a red text block in HTML.</div>

                    ## Escape Characters

                    \*Literal asterisks\*

                    ## Footnotes

                    Here is a footnote reference[^1].

                    [^1]: This is the footnote content.

                    ## Math (if supported by parser)

                    Inline math: $a^2 + b^2 = c^2$

                    Block math:

                    $$
                    \int_0^\infty e^{-x^2} dx = \frac{\sqrt{\pi}}{2}
                    $$

                    """
                ),
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
