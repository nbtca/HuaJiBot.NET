using HuaJiBot.NET.Utils;

namespace HuaJiBot.NET.UnitTest;

internal class MarkdownRenderTest
{
    private const string Table = """
        | Field | Value |
        |-------|-------|
        | Size  | M     |
        """;

    private static string Render(string markdown) =>
        string.Concat(CardBuilder.MarkdownRender(markdown).Select(x => x.Text));

    [Test]
    public void Table_RendersCellsWithoutDelimiterRow() =>
        Assert.That(Render(Table), Is.EqualTo("Field | Value\nSize | M\n"));

    [Test]
    public void Table_MarksHeaderRowBold() =>
        Assert.That(
            CardBuilder.MarkdownRender(Table).First(x => x.Text == "Field").Bold,
            Is.True
        );

    [Test]
    public void HtmlBlock_KeepsTextAndDropsTags() =>
        Assert.That(
            Render("<details>\n<summary>Description</summary>\n</details>"),
            Is.EqualTo("Description\n")
        );

    [Test]
    public void HtmlBlock_WithoutText_YieldsNothing() =>
        Assert.That(Render("<img src=\"https://example.com/a.png\">"), Is.EqualTo("\n"));

    [Test]
    public void HtmlBlock_Comment_IsDropped() =>
        Assert.That(Render("<!-- hidden -->"), Is.EqualTo("\n"));

    [Test]
    public void HtmlInline_IsDropped() =>
        Assert.That(Render("a <b>bold</b> c"), Is.EqualTo("a bold c\n"));

    [Test]
    public void HtmlEntity_IsDecoded() => Assert.That(Render("a &amp; b"), Is.EqualTo("a & b\n"));
}
