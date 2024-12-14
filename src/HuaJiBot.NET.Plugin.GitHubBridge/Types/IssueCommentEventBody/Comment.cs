#nullable disable
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;

internal class Comment
{
    [JsonProperty("url")]
    public Uri Url { get; set; }

    [JsonProperty("html_url")]
    public Uri HtmlUrl { get; set; }

    [JsonProperty("issue_url")]
    public Uri IssueUrl { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("user")]
    public Sender User { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonProperty("author_association")]
    public string AuthorAssociation { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; }

    [JsonProperty("reactions")]
    public Reactions Reactions { get; set; }

    [JsonProperty("performed_via_github_app")]
    public object PerformedViaGithubApp { get; set; }
}
