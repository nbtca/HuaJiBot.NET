using HuaJiBot.NET.Plugin.GitHubBridge;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;

namespace HuaJiBot.NET.UnitTest;

internal class RichMarkdownTest
{
    [Test]
    public void Escape_HtmlSpecialCharacters_UsesEntities() =>
        Assert.That(RichMarkdown.Escape("a<b>&c"), Is.EqualTo("a&lt;b&gt;&amp;c"));

    [Test]
    public void Escape_MarkdownMetacharacters_PrefixesBackslash() =>
        Assert.That(
            RichMarkdown.Escape("*a* _b_ [c](d) `e` ~f~ |g|"),
            Is.EqualTo("\\*a\\* \\_b\\_ \\[c\\]\\(d\\) \\`e\\` \\~f\\~ \\|g\\|")
        );

    [Test]
    public void Truncate_WithinLimit_ReturnsInput() =>
        Assert.That(RichMarkdown.Truncate("abc", 5), Is.EqualTo("abc"));

    [Test]
    public void Truncate_LongerThanLimit_AppendsEllipsis() =>
        Assert.That(RichMarkdown.Truncate("abcdefghij", 5), Is.EqualTo("abcd…"));

    [Test]
    public void Truncate_DoesNotSplitSurrogatePair() =>
        Assert.That(RichMarkdown.Truncate("ab😊cd", 4), Is.EqualTo("ab…"));

    private static IssuesEventBody IssueEvent(string title, string body, params string[] labels) =>
        new()
        {
            Action = "opened",
            Repository = new Repository { FullName = "nbtca/HuaJiBot.NET" },
            Sender = new Sender { Login = "m1ngsama" },
            Issue = new Issue
            {
                Number = 21,
                Title = title,
                Body = body,
                HtmlUrl = new Uri("https://github.com/nbtca/HuaJiBot.NET/issues/21"),
                User = new Sender { Login = "octocat" },
                Labels = [.. labels.Select(x => new Label { Name = x })],
            },
        };

    [Test]
    public void Issue_RendersHeadingLabelsCollapsedBodyAndButton() =>
        Assert.That(
            RichMarkdown.Issue(IssueEvent("Add rich message channel", "Introduce **RichContent**.", "enhancement")).Markdown,
            Is.EqualTo(
                """
                ## nbtca / HuaJiBot.NET

                ### Add rich message channel
                `enhancement` · opened by **m1ngsama**

                <details>
                <summary>Description</summary>

                Introduce **RichContent**.

                </details>

                <tg-button type="url" url="https://github.com/nbtca/HuaJiBot.NET/issues/21">Open issue #21</tg-button>
                """
            )
        );

    [Test]
    public void Issue_WithoutLabels_OmitsLabelRun() =>
        Assert.That(
            RichMarkdown.Issue(IssueEvent("Title", "Body")).Markdown,
            Does.Contain("### Title\nopened by **m1ngsama**")
        );

    [Test]
    public void Issue_WithEmptyBody_OmitsDetailsBlock() =>
        Assert.That(
            RichMarkdown.Issue(IssueEvent("Title", "")).Markdown,
            Does.Not.Contain("<details>")
        );

    [Test]
    public void Issue_EscapesTitleButNotBody() =>
        Assert.That(
            RichMarkdown.Issue(IssueEvent("<b>*x*</b>", "**keep me**")).Markdown,
            Does.Contain("### &lt;b&gt;\\*x\\*&lt;/b&gt;").And.Contains("**keep me**")
        );
}
