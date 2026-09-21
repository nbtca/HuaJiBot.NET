using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

namespace HuaJiBot.NET.UnitTest;

internal class PushSummaryTest
{
    private static Commit Commit(string id, string message) => new() { Id = id + "0000000", Message = message };

    [Test]
    public void MergeCommit_BecomesPullRequestAndIsNotListed()
    {
        var summary = PushSummary.From(
            [
                Commit("aaaaaaa", "fix: first\n\nbody that should not show"),
                Commit("bbbbbbb", "Merge pull request #27 from nbtca/fix/x\n\nfix: empty issue cards"),
            ]
        );

        Assert.Multiple(() =>
        {
            Assert.That(summary.PullRequest, Is.EqualTo(27));
            Assert.That(summary.PullRequestTitle, Is.EqualTo("fix: empty issue cards"));
            Assert.That(summary.Commits, Is.EqualTo(new[] { ("aaaaaaa", "fix: first") }));
        });
    }

    [Test]
    public void MergeCommitWithoutTitle_KeepsNumber()
    {
        var summary = PushSummary.From([Commit("bbbbbbb", "Merge pull request #3 from a/b")]);

        Assert.Multiple(() =>
        {
            Assert.That(summary.PullRequest, Is.EqualTo(3));
            Assert.That(summary.PullRequestTitle, Is.Null);
        });
    }

    [Test]
    public void ManyCommits_ShowsFiveAndCountsTheRest()
    {
        var summary = PushSummary.From(Enumerable.Range(1, 7).Select(i => Commit($"{i}{i}{i}{i}{i}{i}{i}", $"c{i}")).ToList());

        Assert.Multiple(() =>
        {
            Assert.That(summary.PullRequest, Is.Null);
            Assert.That(summary.Commits.Select(x => x.Subject), Is.EqualTo(new[] { "c1", "c2", "c3", "c4", "c5" }));
            Assert.That(summary.Hidden, Is.EqualTo(2));
        });
    }
}
