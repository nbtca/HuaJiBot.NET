using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class PushEventDispatcher
{
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
        plugin.Info("PushEvent " + repositoryFullName);
        {
            if (body.Sender.Login.EndsWith("[bot]"))
                return; //github-actions[bot]\
            #region 文本模式
            //var sb = new StringBuilder();
            //{
            //    var branch = body.Ref.Split('/').Last();
            //    var mainBranch = body.Repository.MasterBranch;
            //    var repoInfo = body.Repository.Name;
            //    if (branch != mainBranch) //如果不是主分支，加上分支名
            //        repoInfo += ":" + branch;
            //    var commitCount = body.Commits.Length;
            //    if (commitCount > 1)
            //        sb.AppendLine($"仓库 {repoInfo} 有 {commitCount} 个新的提交： ");
            //    else
            //        sb.AppendLine($"仓库 {repoInfo} 有新的提交：");
            //}
            //if (body.Commits.Length == 0)
            //{
            //    sb.AppendLine("但是没有文件变更。");
            //    return;
            //}
            //bool showFiles = body.Commits.Length == 1; //如果只有一个提交，显示文件变更
            //foreach (var commit in body.Commits)
            //{
            //    var name = commit.Author.Username;
            //    if (name == "dependabot[bot]")
            //    {
            //        sb.AppendLine($"- 依赖更新[Bot]：{commit.Message}");
            //        continue;
            //    }
            //    var message = commit.Message;
            //    var url = commit.Url;
            //    sb.AppendLine($"{message} by @{name}");
            //    if (showFiles)
            //    {
            //        if (commit.Added.Length > 0)
            //        {
            //            sb.AppendLine($"- {commit.Added.Length} 个文件新增");
            //            foreach (var added in commit.Added.Take(maxChangeCount))
            //                sb.AppendLine($"  {added}");
            //            if (commit.Added.Length > maxChangeCount)
            //                sb.AppendLine("  ...");
            //        }
            //        if (commit.Removed.Length > 0)
            //        {
            //            sb.AppendLine($"- {commit.Removed.Length} 个文件移除");
            //            foreach (var removed in commit.Removed.Take(maxChangeCount))
            //                sb.AppendLine($"  {removed}");
            //            if (commit.Removed.Length > maxChangeCount)
            //                sb.AppendLine("  ...");
            //        }
            //        if (commit.Modified.Length > 0)
            //        {
            //            sb.AppendLine($"- {commit.Modified.Length} 个文件修改");
            //            foreach (var modified in commit.Modified.Take(maxChangeCount))
            //                sb.AppendLine($"  {modified}");
            //            if (commit.Modified.Length > maxChangeCount)
            //                sb.AppendLine("  ...");
            //        }
            //        sb.AppendLine($"{url}");
            //    }
            //    else
            //    {
            //        //如果有多个提交，简要显示文件变更
            //        sb.AppendLine(
            //            $"- {commit.Added.Length} 增，{commit.Removed.Length} 减，{commit.Modified.Length} 改"
            //        );
            //    }
            //}
            //var m = sb.ToString();
            #endregion

            #region 卡片模式
            var branch = body.Ref.Split('/').Last();
            var mainBranch = body.Repository.MasterBranch;
            var repoInfo = body.Repository.FullName.Replace("/", " / ");
            if (branch != mainBranch) //如果不是主分支，加上分支名
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
            // 保存图像到文件
            using var tempImage = card.SaveTempAutoDelete();
            var compareUrl = body.Compare.ToString();
            try
            {
                var result = await plugin.ShortLinkApi.ShortLinkAsync(
                    plugin.Config.ShortLinkApi,
                    compareUrl
                );
                compareUrl = result.Url;
            }
            catch (Exception ex)
            {
                plugin.Error("生成短链接失败：", ex);
            }
            var text = new TextMessage(compareUrl);
            var m = new ImageMessage(tempImage);
            #endregion
            foreach (var group in plugin.GetBroadcastTargets(repositoryFullName))
            {
                await plugin.Service.SendGroupMessageAsync(null, group, m, text);
            }
        }
    }
}
