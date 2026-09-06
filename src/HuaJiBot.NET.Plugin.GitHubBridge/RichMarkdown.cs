using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

namespace HuaJiBot.NET.Plugin.GitHubBridge;

internal static class RichMarkdown
{
    private const string Metacharacters = "\\*_[]()`~|!#";
    private const int BodyLimit = 3000;
    private const int MessageLimit = 200;

    internal static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
            switch (c)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                default:
                    if (Metacharacters.Contains(c))
                        sb.Append('\\');
                    sb.Append(c);
                    break;
            }
        return sb.ToString();
    }

    internal static string Truncate(string? text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max)
            return text ?? "";
        var end = max - 1;
        if (char.IsLowSurrogate(text[end]))
            end--;
        return text[..end] + '…';
    }

    internal static RichContent Issue(IssuesEventBody body) =>
        IssueCard(
            body.Repository,
            body.Issue,
            $"{body.Action} by **{Escape(body.Sender.Login)}**",
            body.Issue.Body,
            "Description"
        );

    internal static RichContent IssueComment(IssueCommentEventBody body) =>
        IssueCard(
            body.Repository,
            body.Issue,
            $"comment {body.Action} by **{Escape(body.Sender.Login)}**",
            body.Comment.Body,
            "Comment"
        );

    private static RichContent IssueCard(
        Repository repository,
        Types.IssuesEventBody.Issue issue,
        string action,
        string? body,
        string summary
    )
    {
        var labels = string.Concat(
            from label in issue.Labels ?? []
            select $"`{Escape(label.Name)}` "
        );
        var sb = new StringBuilder();
        sb.AppendLine(Heading(repository.FullName));
        sb.AppendLine();
        sb.AppendLine($"### {Escape(issue.Title)}");
        sb.AppendLine(labels.Length == 0 ? action : $"{labels}· {action}");
        AppendDetails(sb, summary, body, BodyLimit);
        sb.AppendLine();
        sb.Append(Button(issue.HtmlUrl, $"Open issue #{issue.Number}"));
        return new RichContent(sb.ToString().ReplaceLineEndings("\n"));
    }

    internal static RichContent Push(PushEventBody body)
    {
        var branch = body.Ref.Split('/').Last();
        var heading = Heading(body.Repository.FullName);
        if (branch != body.Repository.MasterBranch)
            heading += " : " + Escape(branch);
        var sb = new StringBuilder();
        sb.AppendLine(heading);
        sb.AppendLine();
        foreach (var commit in body.Commits)
            sb.AppendLine(
                $"- `{commit.Id[..7]}` {Escape(Truncate(commit.Message.ReplaceLineEndings(" "), MessageLimit))} — {Escape(commit.Author.Name)}"
            );
        sb.AppendLine();
        sb.AppendLine($"{Stats(body)}pushed by **{Escape(body.Sender.Login)}**");
        sb.AppendLine();
        sb.Append(Button(body.Compare, "View comparison"));
        return new RichContent(sb.ToString().ReplaceLineEndings("\n"));
    }

    private static string Stats(PushEventBody body)
    {
        var added = body.Commits.Sum(x => x.Added.Length);
        var removed = body.Commits.Sum(x => x.Removed.Length);
        var modified = body.Commits.Sum(x => x.Modified.Length);
        var parts = new List<string>();
        if (added > 0)
            parts.Add($"+{added}");
        if (removed > 0)
            parts.Add($"-{removed}");
        if (modified > 0)
            parts.Add($"~{modified}");
        return parts.Count == 0 ? "" : $"`{string.Join(' ', parts)}` · ";
    }

    private static string Heading(string fullName) =>
        $"## {Escape(fullName).Replace("/", " / ")}";

    private static string Button(Uri url, string text) =>
        $"""<tg-button type="url" url="{url}">{text}</tg-button>""";

    private static void AppendDetails(StringBuilder sb, string summary, string? body, int limit)
    {
        if (string.IsNullOrWhiteSpace(body))
            return;
        sb.AppendLine();
        sb.AppendLine("<details>");
        sb.AppendLine($"<summary>{Escape(summary)}</summary>");
        sb.AppendLine();
        sb.AppendLine(Truncate(body.ReplaceLineEndings("\n"), limit));
        sb.AppendLine();
        sb.AppendLine("</details>");
    }
}
