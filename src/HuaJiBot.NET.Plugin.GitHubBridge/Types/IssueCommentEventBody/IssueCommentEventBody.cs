#nullable disable
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.IssueCommentEventBody;

internal class IssueCommentEventBody : EventBody
{
    [JsonProperty("action")]
    public string Action { get; set; }

    [JsonProperty("issue")]
    public Issue Issue { get; set; }

    [JsonProperty("comment")]
    public Comment Comment { get; set; }

    [JsonProperty("repository")]
    public Repository Repository { get; set; }

    [JsonProperty("organization")]
    public Organization Organization { get; set; }

    [JsonProperty("sender")]
    public Sender Sender { get; set; }
}
