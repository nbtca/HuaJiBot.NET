#nullable disable
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;

internal class WorkflowRun
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("head_branch")]
    public string HeadBranch { get; set; }

    [JsonProperty("head_sha")]
    public string HeadSha { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; }

    [JsonProperty("run_number")]
    public long RunNumber { get; set; }

    [JsonProperty("event")]
    public string Event { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("conclusion")]
    public object Conclusion { get; set; }

    [JsonProperty("workflow_id")]
    public long WorkflowId { get; set; }

    [JsonProperty("check_suite_id")]
    public long CheckSuiteId { get; set; }

    [JsonProperty("check_suite_node_id")]
    public string CheckSuiteNodeId { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }

    [JsonProperty("html_url")]
    public Uri HtmlUrl { get; set; }

    [JsonProperty("pull_requests")]
    public object[] PullRequests { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonProperty("actor")]
    public Sender Actor { get; set; }

    [JsonProperty("run_attempt")]
    public long RunAttempt { get; set; }

    [JsonProperty("referenced_workflows")]
    public object[] ReferencedWorkflows { get; set; }

    [JsonProperty("run_started_at")]
    public DateTimeOffset RunStartedAt { get; set; }

    [JsonProperty("triggering_actor")]
    public Sender TriggeringActor { get; set; }

    [JsonProperty("jobs_url")]
    public Uri JobsUrl { get; set; }

    [JsonProperty("logs_url")]
    public Uri LogsUrl { get; set; }

    [JsonProperty("check_suite_url")]
    public Uri CheckSuiteUrl { get; set; }

    [JsonProperty("artifacts_url")]
    public Uri ArtifactsUrl { get; set; }

    [JsonProperty("cancel_url")]
    public Uri CancelUrl { get; set; }

    [JsonProperty("rerun_url")]
    public Uri RerunUrl { get; set; }

    [JsonProperty("previous_attempt_url")]
    public object PreviousAttemptUrl { get; set; }

    [JsonProperty("workflow_url")]
    public Uri WorkflowUrl { get; set; }

    [JsonProperty("head_commit")]
    public HeadCommit HeadCommit { get; set; }

    [JsonProperty("repository")]
    public HeadRepositoryClass Repository { get; set; }

    [JsonProperty("head_repository")]
    public HeadRepositoryClass HeadRepository { get; set; }
}
