using System.Runtime;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class IssuesEventDispatcher
{
    public static async Task DispatchIssuesEventAsync(this PluginMain plugin, IssuesEventBody body)
    {
        var repositoryFullName = body.Repository.FullName;
        plugin.Info("IssuesEvent " + repositoryFullName);
        //var issue = body.Issue;
        //var (shortLinkException, shortLink) = await plugin.ShortLinkApi.TryShortLinkAsync(
        //    plugin.Config.ShortLinkApi,
        //    issue.HtmlUrl.ToString()
        //);
        //if (shortLinkException is not null)
        //{
        //    plugin.Error("ShortLink Error", shortLinkException);
        //}
        //var targets = plugin.GetBroadcastTargets(issue.RepositoryUrl.ToString());
        //var message = new StringBuilder();
        //message.AppendLine($"[{issue.RepositoryUrl}] {issue.Title}");
        //message.AppendLine($"状态: {issue.State}");
        //message.AppendLine($"链接: {shortLink.Url}");
        //message.AppendLine($"作者: {issue.User.Login}");
        //message.AppendLine($"评论数: {issue.Comments}");
        //message.AppendLine($"标签: {string.Join(", ", issue.Labels.Select(x => x.ToString()))}");
        //message.AppendLine($"[查看详情]({shortLink.Url})");
        //foreach (var target in targets)
        //{
        //    await plugin.Service.SendGroupMessageAsync(null, target, message.ToString());
        //}
        var repoInfo = body.Repository.FullName.Replace("/", " / ");
        var issue = body.Issue;
        var avatar = await Utils.AvatarHelper.GetAsync($"{body.Sender.AvatarUrl}?s=96");
        if (body.Action is "assigned")
        {
            return;
        }
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
        CardBuilder card =
            new()
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
                TopRightContent =
                [
                    //new TextRun(" +2", Color.ParseHex("#2cbe4e")),
                    //new TextRun(" -3", Color.ParseHex("#eb2431")),
                    //new TextRun(" ~4", Color.ParseHex("#ffc000")),
                ],
            }; // 保存图像到文件
        using var tempImage = card.SaveTempAutoDelete(true);
        var issueUrl = issue.HtmlUrl.ToString();
        //try
        //{
        //    var result = await plugin.ShortLinkApi.ShortLinkAsync(
        //        plugin.Config.ShortLinkApi,
        //        issueUrl
        //    );
        //    issueUrl = result.Url;
        //}
        //catch (Exception ex)
        //{
        //    plugin.Error("生成短链接失败：", ex);
        //}
        var text = new TextMessage(issueUrl);
        var m = new ImageMessage(tempImage);
        foreach (var group in plugin.GetBroadcastTargets(repositoryFullName))
        {
            await plugin.Service.SendGroupMessageAsync(null, group, m, text);
        }
    }
}
