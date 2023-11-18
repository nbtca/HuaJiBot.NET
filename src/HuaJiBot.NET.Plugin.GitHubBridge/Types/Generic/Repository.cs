#nullable disable
using HuaJiBot.NET.Plugin.GitHubBridge.Types.PushEventBody;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;

internal class Repository
{
    [JsonProperty("allow_forking")]
    public bool AllowForking { get; set; }

    [JsonProperty("archive_url")]
    public string ArchiveUrl { get; set; }

    [JsonProperty("archived")]
    public bool Archived { get; set; }

    [JsonProperty("assignees_url")]
    public string AssigneesUrl { get; set; }

    [JsonProperty("blobs_url")]
    public string BlobsUrl { get; set; }

    [JsonProperty("branches_url")]
    public string BranchesUrl { get; set; }

    [JsonProperty("clone_url")]
    public Uri CloneUrl { get; set; }

    [JsonProperty("collaborators_url")]
    public string CollaboratorsUrl { get; set; }

    [JsonProperty("comments_url")]
    public string CommentsUrl { get; set; }

    [JsonProperty("commits_url")]
    public string CommitsUrl { get; set; }

    [JsonProperty("compare_url")]
    public string CompareUrl { get; set; }

    [JsonProperty("contents_url")]
    public string ContentsUrl { get; set; }

    [JsonProperty("contributors_url")]
    public Uri ContributorsUrl { get; set; }

    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }

    [JsonProperty("custom_properties")]
    public CustomProperties CustomProperties { get; set; }

    [JsonProperty("default_branch")]
    public string DefaultBranch { get; set; }

    [JsonProperty("deployments_url")]
    public Uri DeploymentsUrl { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("disabled")]
    public bool Disabled { get; set; }

    [JsonProperty("downloads_url")]
    public Uri DownloadsUrl { get; set; }

    [JsonProperty("events_url")]
    public Uri EventsUrl { get; set; }

    [JsonProperty("fork")]
    public bool Fork { get; set; }

    [JsonProperty("forks")]
    public long Forks { get; set; }

    [JsonProperty("forks_count")]
    public long ForksCount { get; set; }

    [JsonProperty("forks_url")]
    public Uri ForksUrl { get; set; }

    [JsonProperty("full_name")]
    public string FullName { get; set; }

    [JsonProperty("git_commits_url")]
    public string GitCommitsUrl { get; set; }

    [JsonProperty("git_refs_url")]
    public string GitRefsUrl { get; set; }

    [JsonProperty("git_tags_url")]
    public string GitTagsUrl { get; set; }

    [JsonProperty("git_url")]
    public string GitUrl { get; set; }

    [JsonProperty("has_discussions")]
    public bool HasDiscussions { get; set; }

    [JsonProperty("has_downloads")]
    public bool HasDownloads { get; set; }

    [JsonProperty("has_issues")]
    public bool HasIssues { get; set; }

    [JsonProperty("has_pages")]
    public bool HasPages { get; set; }

    [JsonProperty("has_projects")]
    public bool HasProjects { get; set; }

    [JsonProperty("has_wiki")]
    public bool HasWiki { get; set; }

    [JsonProperty("homepage")]
    public object Homepage { get; set; }

    [JsonProperty("hooks_url")]
    public Uri HooksUrl { get; set; }

    [JsonProperty("html_url")]
    public Uri HtmlUrl { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("is_template")]
    public bool IsTemplate { get; set; }

    [JsonProperty("issue_comment_url")]
    public string IssueCommentUrl { get; set; }

    [JsonProperty("issue_events_url")]
    public string IssueEventsUrl { get; set; }

    [JsonProperty("issues_url")]
    public string IssuesUrl { get; set; }

    [JsonProperty("keys_url")]
    public string KeysUrl { get; set; }

    [JsonProperty("labels_url")]
    public string LabelsUrl { get; set; }

    [JsonProperty("language")]
    public string Language { get; set; }

    [JsonProperty("languages_url")]
    public Uri LanguagesUrl { get; set; }

    [JsonProperty("license")]
    public License License { get; set; }

    [JsonProperty("master_branch")]
    public string MasterBranch { get; set; }

    [JsonProperty("merges_url")]
    public Uri MergesUrl { get; set; }

    [JsonProperty("milestones_url")]
    public string MilestonesUrl { get; set; }

    [JsonProperty("mirror_url")]
    public object MirrorUrl { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("notifications_url")]
    public string NotificationsUrl { get; set; }

    [JsonProperty("open_issues")]
    public long OpenIssues { get; set; }

    [JsonProperty("open_issues_count")]
    public long OpenIssuesCount { get; set; }

    [JsonProperty("organization")]
    public string Organization { get; set; }

    [JsonProperty("owner")]
    public Sender Owner { get; set; }

    [JsonProperty("private")]
    public bool Private { get; set; }

    [JsonProperty("pulls_url")]
    public string PullsUrl { get; set; }

    [JsonProperty("pushed_at")]
    public long PushedAt { get; set; }

    [JsonProperty("releases_url")]
    public string ReleasesUrl { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("ssh_url")]
    public string SshUrl { get; set; }

    [JsonProperty("stargazers")]
    public long Stargazers { get; set; }

    [JsonProperty("stargazers_count")]
    public long StargazersCount { get; set; }

    [JsonProperty("stargazers_url")]
    public Uri StargazersUrl { get; set; }

    [JsonProperty("statuses_url")]
    public string StatusesUrl { get; set; }

    [JsonProperty("subscribers_url")]
    public Uri SubscribersUrl { get; set; }

    [JsonProperty("subscription_url")]
    public Uri SubscriptionUrl { get; set; }

    [JsonProperty("svn_url")]
    public Uri SvnUrl { get; set; }

    [JsonProperty("tags_url")]
    public Uri TagsUrl { get; set; }

    [JsonProperty("teams_url")]
    public Uri TeamsUrl { get; set; }

    [JsonProperty("topics")]
    public object[] Topics { get; set; }

    [JsonProperty("trees_url")]
    public string TreesUrl { get; set; }

    [JsonProperty("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }

    [JsonProperty("visibility")]
    public string Visibility { get; set; }

    [JsonProperty("watchers")]
    public long Watchers { get; set; }

    [JsonProperty("watchers_count")]
    public long WatchersCount { get; set; }

    [JsonProperty("web_commit_signoff_required")]
    public bool WebCommitSignoffRequired { get; set; }
}
