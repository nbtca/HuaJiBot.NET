using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class IssuesEventDispatcher
{
    // Skip edited, labeled, etc.: ticking a task-list checkbox fires issues.edited.
    internal static bool ShouldBroadcast(IssuesEventBody body) =>
        body.Action is "opened" or "closed" or "reopened" && !Broadcast.IsBot(body.Sender);

    internal static bool ShouldBroadcast(IssueCommentEventBody body) =>
        body.Action is "created" && !Broadcast.IsBot(body.Sender);

    public static async Task DispatchIssueCommentEventAsync(
        this PluginMain plugin,
        IssueCommentEventBody body
    )
    {
        var repositoryFullName = body.Repository.FullName;
        plugin.Info($"IssueCommentEvent {repositoryFullName} {body.Action}");
        if (!ShouldBroadcast(body))
            return;
        TempFile.AutoDeleteFile? tempImage = null;
        try
        {
            await Broadcast.SendAsync(
                plugin.Service,
                plugin.GetBroadcastTargets(body.Repository),
                RichMarkdown.IssueComment(body),
                async () =>
                    [
                        new ImageMessage(tempImage = await BuildCommentCardAsync(body)),
                        new TextMessage(await plugin.OrRawAsync(body.Issue.HtmlUrl)),
                    ]
            );
        }
        finally
        {
            tempImage?.Dispose();
        }
    }

    public static async Task DispatchIssuesEventAsync(this PluginMain plugin, IssuesEventBody body)
    {
        var repositoryFullName = body.Repository.FullName;
        plugin.Info($"IssuesEvent {repositoryFullName} {body.Action}");
        if (!ShouldBroadcast(body))
            return;
        TempFile.AutoDeleteFile? tempImage = null;
        try
        {
            await Broadcast.SendAsync(
                plugin.Service,
                plugin.GetBroadcastTargets(body.Repository),
                RichMarkdown.Issue(body),
                async () =>
                    [
                        new ImageMessage(tempImage = await BuildIssueCardAsync(body)),
                        new TextMessage(await plugin.OrRawAsync(body.Issue.HtmlUrl)),
                    ]
            );
        }
        finally
        {
            tempImage?.Dispose();
        }
    }

    private static Task<TempFile.AutoDeleteFile> BuildCommentCardAsync(IssueCommentEventBody body) =>
        BuildCardAsync(
            body.Repository,
            body.Issue,
            body.Sender,
            body.Issue.State,
            body.Comment.Body,
            $"@{body.Sender.Login} {body.Action} comment."
        );

    private static Task<TempFile.AutoDeleteFile> BuildIssueCardAsync(IssuesEventBody body) =>
        BuildCardAsync(
            body.Repository,
            body.Issue,
            body.Sender,
            body.Action is "opened" or "reopened" ? "open" : body.Action,
            body.Issue.Body,
            $"@{body.Sender.Login} {body.Action} issue"
        );

    /// <param name="state">"open" or "closed"; anything else shows the skipped icon.</param>
    private static async Task<TempFile.AutoDeleteFile> BuildCardAsync(
        Repository repository,
        Issue issue,
        Sender sender,
        string state,
        string? content,
        string footer
    )
    {
        var avatar = await AvatarHelper.GetAsync($"{sender.AvatarUrl}?s=96");
        var (glyph, color) = state switch
        {
            "open" => (IconFonts.IssueOpened, Color.FromRgb(248, 81, 73)),
            "closed" => (IconFonts.IssueClosed, Color.FromRgb(171, 125, 248)),
            _ => (IconFonts.ActionSkip, Color.FromRgb(145, 152, 161)),
        };
        var gray = Color.FromRgb(139, 148, 158);
        var langColor =
            LangColorsHelper.GetColor(repository.Language, out var c)
            && c is { r: var r, g: var g, b: var b }
                ? Color.FromRgb(r, g, b)
                : gray;
        CardBuilder card = new()
        {
            Title = repository.FullName.Replace("/", " / "),
            Subtitle =
            [
                new("# ", langColor),
                new(issue.Title.Length > 50 ? issue.Title[..50] + "..." : issue.Title, Color.Azure),
                new("  by:", gray) { FontSize = 14 },
                new(issue.User.Login, gray) { FontSize = 14 },
            ],
            Content = CardBuilder.MarkdownRender(content),
            Footer = footer,
            FooterIcon = avatar,
            Icon = CardBuilder.CharToImage(glyph, IconFonts.IcoMoonFont(25), color, 30),
            IconPlaceholder = "",
            TopRightContent = [],
        };
        return card.SaveTempAutoDelete(true);
    }
}
