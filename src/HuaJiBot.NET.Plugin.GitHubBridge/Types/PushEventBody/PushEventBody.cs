#nullable disable
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;

internal class PushEventBody : EventBody
{
    [JsonProperty("after")]
    public string After { get; set; }

    [JsonProperty("base_ref")]
    public object BaseRef { get; set; }

    [JsonProperty("before")]
    public string Before { get; set; }

    [JsonProperty("commits")]
    public Commit[] Commits { get; set; }

    [JsonProperty("compare")]
    public Uri Compare { get; set; }

    [JsonProperty("created")]
    public bool Created { get; set; }

    [JsonProperty("deleted")]
    public bool Deleted { get; set; }

    [JsonProperty("forced")]
    public bool Forced { get; set; }

    [JsonProperty("head_commit")]
    public Commit HeadCommit { get; set; }

    [JsonProperty("organization")]
    public Organization Organization { get; set; }

    [JsonProperty("pusher")]
    public Pusher Pusher { get; set; }

    [JsonProperty("ref")]
    public string Ref { get; set; }

    [JsonProperty("repository")]
    public Repository Repository { get; set; }

    [JsonProperty("sender")]
    public Sender Sender { get; set; }
}
