#nullable disable
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

internal class Commit
{
    [JsonProperty("modified")]
    public string[] Modified { get; set; }

    [JsonProperty("removed")]
    public string[] Removed { get; set; }

    [JsonProperty("added")]
    public string[] Added { get; set; }

    [JsonProperty("author")]
    public Author Author { get; set; }

    [JsonProperty("committer")]
    public Author Committer { get; set; }

    [JsonProperty("distinct")]
    public bool Distinct { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonProperty("tree_id")]
    public string TreeId { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }
}
