using System.Text.RegularExpressions;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

/// <summary>What a push card shows: the merged pull request, if any, and commit subjects.</summary>
internal sealed partial record PushSummary(
    int? PullRequest,
    string? PullRequestTitle,
    IReadOnlyList<(string Sha, string Subject)> Commits,
    int Hidden
)
{
    internal const int MaxCommits = 5;

    [GeneratedRegex(@"^Merge pull request #(?<number>\d+) from \S+[ \t]*(?:\n[ \t]*\n(?<title>[^\n]+))?")]
    private static partial Regex MergeCommit();

    internal static PushSummary From(IReadOnlyList<Commit> commits)
    {
        int? number = null;
        string? title = null;
        var shown = new List<(string, string)>();
        foreach (var commit in commits)
        {
            var message = commit.Message.ReplaceLineEndings("\n");
            if (MergeCommit().Match(message) is { Success: true } match)
            {
                number = int.Parse(match.Groups["number"].Value);
                title = match.Groups["title"] is { Success: true } t ? t.Value.Trim() : null;
                continue;
            }
            shown.Add((commit.Id[..7], message.Split('\n')[0].Trim()));
        }
        return new(
            number,
            title,
            shown.Take(MaxCommits).ToList(),
            Math.Max(0, shown.Count - MaxCommits)
        );
    }
}
