using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

namespace HuaJiBot.NET.UnitTest;

internal class GitHubPostTest
{
    private static readonly Repository Repo = new() { Name = "HuaJiBot.NET", DefaultBranch = "main" };
    private static readonly Sender User = new() { Login = "m1ng_sama" };
    private static readonly Func<Task<string>> Card = () => Task.FromResult("card.png");
    private static readonly Func<Task<SendingMessageBase[]>> Old = () =>
        Task.FromResult<SendingMessageBase[]>([new TextMessage("old")]);

    private static Issue Issue52 =>
        new() { Number = 52, HtmlUrl = new("https://github.com/nbtca/HuaJiBot.NET/issues/52") };

    [TestCase("opened", "打开了")]
    [TestCase("closed", "关闭了")]
    [TestCase("reopened", "重新打开了")]
    public void Issue_DescribesActionAndLinksIssue(string action, string verb)
    {
        var post = GitHubPosts.Issue(
            new IssuesEventBody { Action = action, Sender = User, Repository = Repo, Issue = Issue52 },
            Card,
            Old
        );

        Assert.Multiple(() =>
        {
            Assert.That(post.Markdown, Is.EqualTo($"m1ng\\_sama {verb} #52"));
            Assert.That(post.Tags, Is.EqualTo(new[] { "issue", "HuaJiBot.NET" }));
            Assert.That(
                post.Links,
                Is.EqualTo(new[] { new LinkMessage("打开 #52", "https://github.com/nbtca/HuaJiBot.NET/issues/52") })
            );
            Assert.That(post.Image, Is.SameAs(Card));
            Assert.That(post.Fallback, Is.SameAs(Old));
        });
    }

    [Test]
    public void Comment_LinksTheComment()
    {
        var post = GitHubPosts.Comment(
            new IssueCommentEventBody
            {
                Action = "created",
                Sender = User,
                Repository = Repo,
                Issue = Issue52,
                Comment = new() { HtmlUrl = new("https://github.com/nbtca/HuaJiBot.NET/issues/52#issuecomment-1") },
            },
            Card,
            Old
        );

        Assert.Multiple(() =>
        {
            Assert.That(post.Markdown, Is.EqualTo("m1ng\\_sama 评论了 #52"));
            Assert.That(post.Tags, Is.EqualTo(new[] { "comment", "HuaJiBot.NET" }));
            Assert.That(post.Links.Single().Url, Does.EndWith("#issuecomment-1"));
        });
    }

    [Test]
    public void Push_CountsCommitsOnBranch()
    {
        var post = GitHubPosts.Push(
            new PushEventBody
            {
                Ref = "refs/heads/main",
                Sender = User,
                Repository = Repo,
                Commits = [new(), new(), new()],
                Compare = new("https://github.com/nbtca/HuaJiBot.NET/compare/a...b"),
            },
            Card,
            Old
        );

        Assert.Multiple(() =>
        {
            Assert.That(post.Markdown, Is.EqualTo("m1ng\\_sama 向 main 推送了 3 个提交"));
            Assert.That(post.Tags, Is.EqualTo(new[] { "push", "HuaJiBot.NET" }));
            Assert.That(post.Links.Single(), Is.EqualTo(new LinkMessage("查看改动", "https://github.com/nbtca/HuaJiBot.NET/compare/a...b")));
        });
    }

    [Test]
    public void Excerpt_KeepsShortBody() =>
        Assert.That(GitHubPosts.Excerpt("one\ntwo"), Is.EqualTo("one\ntwo"));

    [Test]
    public void Excerpt_CutsManyLinesToThree() =>
        Assert.That(GitHubPosts.Excerpt("1\n2\n3\n4\n5\n6\n7"), Is.EqualTo("1\n2\n3…"));

    [Test]
    public void Excerpt_CutsLongParagraph()
    {
        var excerpt = GitHubPosts.Excerpt(new string('a', 500))!;
        Assert.Multiple(() =>
        {
            Assert.That(excerpt, Has.Length.EqualTo(GitHubPosts.ExcerptLimit));
            Assert.That(excerpt, Does.EndWith("…"));
        });
    }
}
