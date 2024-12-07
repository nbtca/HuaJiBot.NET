using System.Text;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class IssuesEventDispatcher
{
    public static async Task DispatchIssuesEventAsync(this PluginMain plugin, IssuesEventBody body)
    {
        var issue = body.Issue;
        var (shortLinkException, shortLink) = await plugin.ShortLinkApi.TryShortLinkAsync(
            plugin.Config.ShortLinkApi,
            issue.HtmlUrl.ToString()
        );
        if (shortLinkException is not null)
        {
            plugin.Error("ShortLink Error", shortLinkException);
        }
        var targets = plugin.GetBroadcastTargets(issue.RepositoryUrl.ToString());
        var message = new StringBuilder();
        message.AppendLine($"[{issue.RepositoryUrl}] {issue.Title}");
        message.AppendLine($"状态: {issue.State}");
        message.AppendLine($"链接: {shortLink.Url}");
        message.AppendLine($"作者: {issue.User.Login}");
        message.AppendLine($"评论数: {issue.Comments}");
        message.AppendLine($"标签: {string.Join(", ", issue.Labels.Select(x => x.ToString()))}");
        message.AppendLine($"[查看详情]({shortLink.Url})");
        foreach (var target in targets)
        {
            await plugin.Service.SendGroupMessageAsync(null, target, message.ToString());
        }
    }
}
