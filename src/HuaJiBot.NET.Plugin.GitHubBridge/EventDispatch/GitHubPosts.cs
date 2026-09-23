using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class GitHubPosts
{
    internal const int ExcerptLimit = 280;

    internal static Post Issue(
        IssuesEventBody body,
        Func<Task<string>> card,
        Func<Task<SendingMessageBase[]>> fallback
    ) =>
        new(
            $"{Post.Escape(body.Sender.Login)} {Verb(body.Action)} #{body.Issue.Number}",
            fallback
        )
        {
            Image = card,
            Tags = ["issue", body.Repository.Name],
            Links = [new($"打开 #{body.Issue.Number}", body.Issue.HtmlUrl.ToString())],
        };

    internal static Post Comment(
        IssueCommentEventBody body,
        Func<Task<string>> card,
        Func<Task<SendingMessageBase[]>> fallback
    ) =>
        new($"{Post.Escape(body.Sender.Login)} 评论了 #{body.Issue.Number}", fallback)
        {
            Image = card,
            Tags = ["comment", body.Repository.Name],
            Links = [new("查看评论", body.Comment.HtmlUrl.ToString())],
        };

    internal static Post Push(
        PushEventBody body,
        Func<Task<string>> card,
        Func<Task<SendingMessageBase[]>> fallback
    ) =>
        new(
            $"{Post.Escape(body.Sender.Login)} 向 {Post.Escape(body.Ref.Split('/').Last())} 推送了 {body.Commits.Length} 个提交",
            fallback
        )
        {
            Image = card,
            Tags = ["push", body.Repository.Name],
            Links = [new("查看改动", body.Compare.ToString())],
        };

    internal static string? Excerpt(string? body)
    {
        if (body is null)
            return null;
        var text = body.ReplaceLineEndings("\n").Trim();
        var lines = text.Split('\n');
        if (lines.Length <= 6 && text.Length <= ExcerptLimit)
            return text;
        var head = string.Join('\n', lines.Take(3));
        if (lines.Length > 3 && head.Length < ExcerptLimit)
            return head + '…';
        var end = ExcerptLimit - 1;
        if (char.IsLowSurrogate(head[end]))
            end--;
        return head[..end] + '…';
    }

    private static string Verb(string action) =>
        action switch
        {
            "opened" => "打开了",
            "closed" => "关闭了",
            "reopened" => "重新打开了",
            _ => action,
        };
}
