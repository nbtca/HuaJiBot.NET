#nullable disable
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;

internal class Sender
{
    [JsonProperty("avatar_url")]
    public Uri AvatarUrl { get; set; }

    [JsonProperty("email")]
    public object Email { get; set; }

    [JsonProperty("events_url")]
    public string EventsUrl { get; set; }

    [JsonProperty("followers_url")]
    public Uri FollowersUrl { get; set; }

    [JsonProperty("following_url")]
    public string FollowingUrl { get; set; }

    [JsonProperty("gists_url")]
    public string GistsUrl { get; set; }

    [JsonProperty("gravatar_id")]
    public string GravatarId { get; set; }

    [JsonProperty("html_url")]
    public Uri HtmlUrl { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("login")]
    public string Login { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("node_id")]
    public string NodeId { get; set; }

    [JsonProperty("organizations_url")]
    public Uri OrganizationsUrl { get; set; }

    [JsonProperty("received_events_url")]
    public Uri ReceivedEventsUrl { get; set; }

    [JsonProperty("repos_url")]
    public Uri ReposUrl { get; set; }

    [JsonProperty("site_admin")]
    public bool SiteAdmin { get; set; }

    [JsonProperty("starred_url")]
    public string StarredUrl { get; set; }

    [JsonProperty("subscriptions_url")]
    public Uri SubscriptionsUrl { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }
}
