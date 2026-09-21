using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class PushEventDispatcher
{
    // Feature and PR branch pushes are too frequent to broadcast.
    internal static bool ShouldBroadcast(PushEventBody body) =>
        body.Ref == $"refs/heads/{body.Repository.DefaultBranch}" && !Broadcast.IsBot(body.Sender);

    public static async Task DispatchPushEventAsync(this PluginMain plugin, PushEventBody body)
    {
        //排除如workflow的提交
        if (
            body.Commits.All(x =>
                x.Modified.All(y => y.StartsWith("."))
                && x.Added.All(y => y.StartsWith("."))
                && x.Removed.All(y => y.StartsWith("."))
            )
        )
        {
            return;
        }
        var repositoryFullName = body.Repository.FullName;
        plugin.Info($"PushEvent {repositoryFullName} {body.Ref}");
        {
            if (!ShouldBroadcast(body))
                return;

            TempFile.AutoDeleteFile? tempImage = null;
            try
            {
                await Broadcast.SendAsync(
                    plugin.Service,
                    plugin.GetBroadcastTargets(body.Repository),
                    RichMarkdown.Push(body),
                    async () =>
                        [
                            new ImageMessage(tempImage = await BuildPushCardAsync(body)),
                            new TextMessage(await plugin.OrRawAsync(body.Compare)),
                        ]
                );
            }
            finally
            {
                tempImage?.Dispose();
            }
        }
    }

    private static async Task<TempFile.AutoDeleteFile> BuildPushCardAsync(PushEventBody body)
    {
        var branch = body.Ref.Split('/').Last();
        var mainBranch = body.Repository.MasterBranch;
        var repoInfo = body.Repository.FullName.Replace("/", " / ");
        if (branch != mainBranch)
            repoInfo += " : " + branch;
        var avatar = await Utils.AvatarHelper.GetAsync($"{body.Sender.AvatarUrl}?s=96");
        var editInfo = new List<TextRun>();
        var addCount = 0;
        var removeCount = 0;
        var modifyCount = 0;
        var commitList = new List<string>();
        foreach (var commit in body.Commits)
        {
            addCount += commit.Added.Length;
            removeCount += commit.Removed.Length;
            modifyCount += commit.Modified.Length;
            commitList.Add(commit.Id[..7]);
        }
        if (addCount > 0)
            editInfo.Add(new TextRun($" +{addCount}", Color.ParseHex("#2cbe4e")));
        if (removeCount > 0)
            editInfo.Add(new TextRun($" -{removeCount}", Color.ParseHex("#eb2431")));
        if (modifyCount > 0)
            editInfo.Add(new TextRun($" ~{modifyCount}", Color.ParseHex("#ffc000")));

        CardBuilder card =
            new()
            {
                Title = repoInfo,
                Subtitle = new Func<IEnumerable<TextRun>>(() =>
                {
                    var font = IconFonts.IcoMoonFont(19);
                    var lang = body.Repository.Language;
                    var subtitleColor = Color.FromRgb(139, 148, 158);
                    var list = new List<TextRun>();
                    if (lang is not null)
                    {
                        list.Add(
                            new(
                                IconFonts.IconCircle,
                                LangColorsHelper.GetColor(lang, out var color)
                                && color is { r: var r, g: var g, b: var b }
                                    ? Color.FromRgb(r, g, b)
                                    : subtitleColor,
                                font
                            )
                        );
                        list.Add(" ");
                        list.Add(new(lang, subtitleColor));
                    }

                    void Add(char icon, string text)
                    {
                        list.Add("  ");
                        list.Add(new(icon, subtitleColor, font));
                        list.Add(" ");
                        list.Add(new(text, subtitleColor));
                    }
                    if (body.Repository.StargazersCount is var stars)
                        Add(IconFonts.IconStars, stars.ToString());
                    if (body.Repository.ForksCount is var forks)
                        Add(IconFonts.IconForks, forks.ToString());
                    if (body.Repository.OpenIssuesCount is var issues)
                        Add(IconFonts.IconIssues, issues.ToString());
                    if (body.Repository.License is { SpdxId: var license })
                        Add(IconFonts.IconLaw, license);
                    return list;
                }).Invoke(),
                Content = (
                    from x in body.Commits
                    select (IEnumerable<TextRun>)
                        [
                            new(x.Message),
                            new($" by @{x.Author.Name}", Color.Gray),
                            Environment.NewLine,
                        ]
                ).Aggregate((a, b) => a.Concat(b).ToArray()),
                Footer =
                    $"@{body.Sender.Login} pushed {body.Commits.Length} commit{(body.Commits.Length > 1 ? "s" : "")}.",
                FooterIcon = avatar,
                Icon = CardBuilder.CharToImage(
                    IconFonts.IconCommit,
                    IconFonts.IcoMoonFont(30),
                    Color.White
                ),
                IconPlaceholder = string.Join(Environment.NewLine, commitList),
                TopRightContent = editInfo,
            };
        return card.SaveTempAutoDelete();
    }
}
