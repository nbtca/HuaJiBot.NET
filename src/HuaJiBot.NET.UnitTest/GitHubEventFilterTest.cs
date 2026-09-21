using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

namespace HuaJiBot.NET.UnitTest;

internal class GitHubEventFilterTest
{
    private static readonly Sender User = new() { Login = "octocat", Type = "User" };
    private static readonly Sender Bot = new() { Login = "github-actions[bot]", Type = "Bot" };

    [TestCase("opened", true)]
    [TestCase("closed", true)]
    [TestCase("reopened", true)]
    [TestCase("edited", false)]
    [TestCase("labeled", false)]
    [TestCase("assigned", false)]
    public void Issues_OnlyStateChangesAreBroadcast(string action, bool expected)
    {
        var body = new IssuesEventBody { Action = action, Sender = User };
        Assert.That(IssuesEventDispatcher.ShouldBroadcast(body), Is.EqualTo(expected));
    }

    [TestCase("created", true)]
    [TestCase("edited", false)]
    [TestCase("deleted", false)]
    public void IssueComments_OnlyNewCommentsAreBroadcast(string action, bool expected)
    {
        var body = new IssueCommentEventBody { Action = action, Sender = User };
        Assert.That(IssuesEventDispatcher.ShouldBroadcast(body), Is.EqualTo(expected));
    }

    [Test]
    public void Issues_FromBotsAreNotBroadcast()
    {
        var body = new IssuesEventBody { Action = "opened", Sender = Bot };
        Assert.That(IssuesEventDispatcher.ShouldBroadcast(body), Is.False);
    }

    [TestCase("refs/heads/main", true)]
    [TestCase("refs/heads/feat/x", false)]
    [TestCase("refs/tags/v1.0", false)]
    public void Push_OnlyDefaultBranchIsBroadcast(string @ref, bool expected)
    {
        var body = new PushEventBody
        {
            Ref = @ref,
            Repository = new Repository { DefaultBranch = "main" },
            Sender = User,
        };
        Assert.That(PushEventDispatcher.ShouldBroadcast(body), Is.EqualTo(expected));
    }
}
