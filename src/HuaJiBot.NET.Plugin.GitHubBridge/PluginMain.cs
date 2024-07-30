using System.Net.WebSockets;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.Types;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;
using HuaJiBot.NET.Plugin.GitHubBridge.Utils;
using HuaJiBot.NET.Utils;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using Websocket.Client;

namespace HuaJiBot.NET.Plugin.GitHubBridge;

public class PluginConfig : ConfigBase
{
    public string ShortLinkToken { get; set; } = "";
    public string Address { get; set; } = "ws://localhost:8080";
    public string AuthBearer { get; set; } = "";
    public Dictionary<string, string> BroadcastMap { get; set; } = new();
    public Dictionary<string, string[]> BroadcastGroup { get; set; } =
        new() { ["default"] = ["123456789"] }; //默认的广播目标
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();
    private ShortLinkApi _shortLinkApi = null!;

    private IEnumerable<string> GetBroadcastTargets(string fullName)
    {
        if (!Config.BroadcastMap.TryGetValue(fullName, out var group))
        {
            group = "default";
            Config.BroadcastMap.Add(fullName, group);
            Service.Config.Save();
        }
        if (!Config.BroadcastGroup.TryGetValue(group, out var targets))
        {
            targets = [];
            Config.BroadcastGroup.Add(group, targets);
            Service.Config.Save();
        }
        return targets;
    }

    //处理消息
    private async Task ProcessMessageAsync(string msg)
    {
        var e = JsonConvert.DeserializeObject<Event>(msg)!;
        {
            switch (e.Body)
            {
                case PushEventBody body:
                    {
                        var repositoryFullName = body.Repository.FullName;
                        Info("PushEvent " + repositoryFullName);
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
                            var avatar = await Utils.AvatarHelper.GetAsync(
                                $"{body.Sender.AvatarUrl}?s=96"
                            );
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
                                editInfo.Add(
                                    new TextRun($" +{addCount}", Color.ParseHex("#2cbe4e"))
                                );
                            if (removeCount > 0)
                                editInfo.Add(
                                    new TextRun($" -{removeCount}", Color.ParseHex("#eb2431"))
                                );
                            if (modifyCount > 0)
                                editInfo.Add(
                                    new TextRun($" ~{modifyCount}", Color.ParseHex("#ffc000"))
                                );

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
                                            new TextRun[]
                                            {
                                                new(x.Message),
                                                new($" by @{x.Author.Name}", Color.Gray),
                                                Environment.NewLine
                                            }
                                    ).Aggregate((a, b) => a.Concat(b).ToArray()),
                                    Footer =
                                        $"@{body.Sender.Login} pushed {body.Commits.Length} commit{(body.Commits.Length > 1 ? "s" : "")}.",
                                    FooterIcon = avatar,
                                    Icon = Convert.FromBase64String(
                                        "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAN5JREFUSEvt0zFqAkEUh/HfksPY2FvEwnQiBok38ACSJthqadqkSxUQayFgYWcleoAUOUWOkLCygiy7O4Zlqjjlzs7/m/e9eYnIK4mc7woIGv4/itqYo4EffOENi5CjSxTNMC0JGuG9ChIC9LDOAg54xA2e8JB9H+CjDBICpAfv8YlmLuQFY2xxdwkgdVu2utjkNlvYV5w5Xv68girALXZ1AUWXOSlaYZj74RmTvygqApw3+RVLfGd96aODWk1OoVGf6amqqIMWGtbK/dAc1ArPP9PaYUUB1wqCWqMr+gV7eiMZdZi41AAAAABJRU5ErkJggg=="
                                    ),
                                    IconPlaceholder = string.Join(Environment.NewLine, commitList),
                                    TopRightContent = editInfo
                                };
                            // 保存图像到文件
                            using var tempImage = card.SaveTempAutoDelete();
                            var compareUrl = body.Compare.ToString();
                            try
                            {
                                var result = await _shortLinkApi.ShortLinkAsync(compareUrl);
                                compareUrl = result.Url;
                            }
                            catch (Exception ex)
                            {
                                Error("生成短链接失败：", ex);
                            }
                            var text = new TextMessage(compareUrl);
                            var m = new ImageMessage(tempImage);
                            #endregion
                            foreach (var group in GetBroadcastTargets(repositoryFullName))
                            {
                                Service.SendGroupMessage(null, group, m, text);
                            }
                        }
                    }
                    break;
                case WorkflowRunEventBody body:
                    Info("WorkflowRun");
                    break;
                case UnknownEventBody body:
                    Info("收到未实现的事件！" + e.Headers.XGithubEvent[0]);
                    break;
            }
        }
    }

    //初始化
    protected override async Task InitializeAsync()
    {
        _shortLinkApi = new ShortLinkApi(Config.ShortLinkToken);
        WebsocketClient client =
            new(
                new Uri(Config.Address),
                () =>
                {
                    var client = new ClientWebSocket
                    {
                        Options = { CollectHttpResponseDetails = true, }
                    };
                    client.Options.SetRequestHeader("Authorization", $"Bearer {Config.AuthBearer}");
                    return client;
                }
            )
            {
                IsReconnectionEnabled = true,
                ReconnectTimeout = null,
                MessageEncoding = Encoding.UTF8,
                IsTextMessageConversionEnabled = true,
            };
        client.MessageReceived.Subscribe(msg =>
        {
            if (msg.MessageType == WebSocketMessageType.Text)
            {
                try
                {
                    ProcessMessageAsync(msg.Text ?? throw new NullReferenceException("msg.Text"))
                        .ContinueWith(
                            task =>
                            {
                                var ex = task.Exception;
                                if (ex is not null)
                                    Error("ProcessMessage 处理消息时出现异常：", ex);
                                if (msg.Text is { } raw)
                                    Error("ProcessMessage 处理消息时出现异常Raw：", raw);
                            },
                            TaskContinuationOptions.OnlyOnFaulted
                        );
                }
                catch (Exception e)
                {
                    Error("处理消息时出现异常：", e);
                    if (msg.Text is { } raw)
                        Error("处理消息时出现异常Raw：", raw);
                }
            }
            else
            {
                Info("收到非文本消息！");
            }
        });
        client.DisconnectionHappened.Subscribe(info =>
            Info(
                "Disconnection Happened. Type:"
                    + info.Type
                    + " Description:"
                    + info.CloseStatusDescription
            )
        );
        client.ReconnectionHappened.Subscribe(info => Info("Reconnection Happened " + info.Type));
        await client.Start();
        Info("启动成功！");
    }

    protected override void Unload() { }
}
