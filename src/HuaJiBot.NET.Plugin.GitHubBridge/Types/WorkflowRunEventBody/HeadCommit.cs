#nullable disable
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;

internal class HeadCommit
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("tree_id")]
    public string TreeId { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonProperty("author")]
    public Author Author { get; set; }

    [JsonProperty("committer")]
    public Author Committer { get; set; }
}
