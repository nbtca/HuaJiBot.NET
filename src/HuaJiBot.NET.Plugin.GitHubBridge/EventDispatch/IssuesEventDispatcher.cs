using System.Runtime;
using System.Text;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class IssuesEventDispatcher
{
    public static async Task DispatchIssuesEventAsync(this PluginMain plugin, IssuesEventBody body)
    {
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
        //var repoInfo = body.Repository.FullName.Replace("/", " / ");
        //var issue = body.Issue;
        //CardBuilder card =
        //    new()
        //    {
        //        Title = repoInfo,
        //        Subtitle = new Func<IEnumerable<TextRun>>(() =>
        //        {
        //            var font = IconFonts.IcoMoonFont(19);
        //            var lang = body.Repository.Language;
        //            var subtitleColor = Color.FromRgb(139, 148, 158);
        //            var list = new List<TextRun>();
        //            if (lang is not null)
        //            {
        //                list.Add(
        //                    new(
        //                        IconFonts.IconCircle,
        //                        LangColorsHelper.GetColor(lang, out var color)
        //                        && color is { r: var r, g: var g, b: var b }
        //                            ? Color.FromRgb(r, g, b)
        //                            : subtitleColor,
        //                        font
        //                    )
        //                );
        //                list.Add(" ");
        //                list.Add(new(lang, subtitleColor));
        //            }

        //            void Add(char icon, string text)
        //            {
        //                list.Add("  ");
        //                list.Add(new(icon, subtitleColor, font));
        //                list.Add(" ");
        //                list.Add(new(text, subtitleColor));
        //            }
        //            if (body.Repository.StargazersCount is var stars)
        //                Add(IconFonts.IconStars, stars.ToString());
        //            if (body.Repository.ForksCount is var forks)
        //                Add(IconFonts.IconForks, forks.ToString());
        //            if (body.Repository.OpenIssuesCount is var issues)
        //                Add(IconFonts.IconIssues, issues.ToString());
        //            if (body.Repository.License is { SpdxId: var license })
        //                Add(IconFonts.IconLaw, license);
        //            return list;
        //        }).Invoke(),
        //        Content = (
        //            from x in body.Commits
        //            select (IEnumerable<TextRun>)
        //                new TextRun[]
        //                {
        //                    new(x.Message),
        //                    new($" by @{x.Author.Name}", Color.Gray),
        //                    Environment.NewLine,
        //                }
        //        ).Aggregate((a, b) => a.Concat(b).ToArray()),
        //        Footer =
        //            $"@{body.Sender.Login} pushed {body.Commits.Length} commit{(body.Commits.Length > 1 ? "s" : "")}.",
        //        FooterIcon = avatar,
        //        Icon = Convert.FromBase64String(
        //            "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAN5JREFUSEvt0zFqAkEUh/HfksPY2FvEwnQiBok38ACSJthqadqkSxUQayFgYWcleoAUOUWOkLCygiy7O4Zlqjjlzs7/m/e9eYnIK4mc7woIGv4/itqYo4EffOENi5CjSxTNMC0JGuG9ChIC9LDOAg54xA2e8JB9H+CjDBICpAfv8YlmLuQFY2xxdwkgdVu2utjkNlvYV5w5Xv68girALXZ1AUWXOSlaYZj74RmTvygqApw3+RVLfGd96aODWk1OoVGf6amqqIMWGtbK/dAc1ArPP9PaYUUB1wqCWqMr+gV7eiMZdZi41AAAAABJRU5ErkJggg=="
        //        ),
        //        IconPlaceholder = string.Join(Environment.NewLine, commitList),
        //        TopRightContent = editInfo,
        //    };
    }
}
