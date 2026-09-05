using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class IssuesEventDispatcher
{
    public static async Task DispatchIssueCommentEventAsync(
        this PluginMain plugin,
        IssueCommentEventBody body
    )
    {
        var repositoryFullName = body.Repository.FullName;
        plugin.Info("IssueCommentEvent " + repositoryFullName);
        if (body.Action is "assigned")
        {
            return;
        }
        TempFile.AutoDeleteFile? tempImage = null;
        try
        {
            await Broadcast.SendAsync(
                plugin.Service,
                plugin.GetBroadcastTargets(repositoryFullName),
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

    private static async Task<TempFile.AutoDeleteFile> BuildCommentCardAsync(
        IssueCommentEventBody body
    )
    {
        var repoInfo = body.Repository.FullName.Replace("/", " / ");
        var issue = body.Issue;
        var comment = body.Comment;
        var avatar = await Utils.AvatarHelper.GetAsync($"{body.Sender.AvatarUrl}?s=96");
        var icon = issue.State switch
        {
            "open" => CardBuilder.CharToImage(
                IconFonts.IssueOpened,
                IconFonts.IcoMoonFont(25),
                Color.FromRgb(248, 81, 73),
                30
            ),
            "closed" => CardBuilder.CharToImage(
                IconFonts.IssueClosed,
                IconFonts.IcoMoonFont(25),
                Color.FromRgb(171, 125, 248),
                30
            ),
            _ => CardBuilder.CharToImage(
                IconFonts.ActionSkip,
                IconFonts.IcoMoonFont(25),
                Color.FromRgb(145, 152, 161),
                30
            ) //not plan
            ,
        };
        CardBuilder card = new()
        {
            Title = repoInfo,
            Subtitle = new Func<IEnumerable<TextRun>>(() =>
            {
                var lang = body.Repository.Language;
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
                    new(
                        issue.Title.Length > 50
                            ? issue.Title.Substring(0, 50) + "..."
                            : issue.Title,
                        titleColor
                    ),
                    new("  by:", gray) { FontSize = 14 },
                    new(issue.User.Login, gray) { FontSize = 14 },
                };
            }).Invoke(),
            Content = CardBuilder.MarkdownRender(comment.Body),
            Footer = $"@{body.Sender.Login} {body.Action} comment.",
            FooterIcon = avatar,
            Icon = icon,
            IconPlaceholder = "",
            TopRightContent = [],
        };
        return card.SaveTempAutoDelete(true);
    }

    public static async Task DispatchIssuesEventAsync(this PluginMain plugin, IssuesEventBody body)
    {
        var repositoryFullName = body.Repository.FullName;
        plugin.Info("IssuesEvent " + repositoryFullName);
        if (body.Action is "assigned")
        {
            return;
        }
        TempFile.AutoDeleteFile? tempImage = null;
        try
        {
            await Broadcast.SendAsync(
                plugin.Service,
                plugin.GetBroadcastTargets(repositoryFullName),
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

    private static async Task<TempFile.AutoDeleteFile> BuildIssueCardAsync(IssuesEventBody body)
    {
        var repoInfo = body.Repository.FullName.Replace("/", " / ");
        var issue = body.Issue;
        var avatar = await Utils.AvatarHelper.GetAsync($"{body.Sender.AvatarUrl}?s=96");
        var icon = body.Action switch
        {
            "open" => CardBuilder.CharToImage(
                IconFonts.IssueOpened,
                IconFonts.IcoMoonFont(25),
                Color.FromRgb(248, 81, 73),
                30
            ),
            "closed" => CardBuilder.CharToImage(
                IconFonts.IssueClosed,
                IconFonts.IcoMoonFont(25),
                Color.FromRgb(171, 125, 248),
                30
            ),
            _ => CardBuilder.CharToImage(
                IconFonts.ActionSkip,
                IconFonts.IcoMoonFont(25),
                Color.FromRgb(145, 152, 161),
                30
            ) //not plan
            ,
        };
        CardBuilder card = new()
        {
            Title = repoInfo,
            Subtitle = new Func<IEnumerable<TextRun>>(() =>
            {
                var lang = body.Repository.Language;
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
                    new(
                        issue.Title.Length > 50
                            ? issue.Title.Substring(0, 50) + "..."
                            : issue.Title,
                        titleColor
                    ),
                    new("  by:", gray) { FontSize = 14 },
                    new(issue.User.Login, gray) { FontSize = 14 },
                };
            }).Invoke(),
            Content = CardBuilder.MarkdownRender(issue.Body),
            Footer = $"@{body.Sender.Login} {body.Action} issue",
            FooterIcon = avatar,
            Icon = icon,
            IconPlaceholder = "",
            TopRightContent = [],
        };
        return card.SaveTempAutoDelete(true);
    }
}
