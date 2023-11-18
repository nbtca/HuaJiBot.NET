#nullable disable
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;

internal class Workflow
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }

    [JsonProperty("html_url")]
    public Uri HtmlUrl { get; set; }

    [JsonProperty("badge_url")]
    public Uri BadgeUrl { get; set; }
}
