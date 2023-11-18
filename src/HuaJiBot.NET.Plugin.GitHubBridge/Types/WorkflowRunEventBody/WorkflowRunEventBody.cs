#nullable disable
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;

using Newtonsoft.Json;

internal class WorkflowRunEventBody : EventBody
{
    [JsonProperty("action")]
    public string Action { get; set; }

    [JsonProperty("workflow_run")]
    public WorkflowRun WorkflowRun { get; set; }

    [JsonProperty("workflow")]
    public Workflow Workflow { get; set; }

    [JsonProperty("repository")]
    public Repository Repository { get; set; }

    [JsonProperty("organization")]
    public Organization Organization { get; set; }

    [JsonProperty("sender")]
    public Sender Sender { get; set; }
}
