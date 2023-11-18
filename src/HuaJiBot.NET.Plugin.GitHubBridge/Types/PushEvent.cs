#nullable disable
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types;

internal class Headers
{
    [JsonProperty("Accept")]
    public string[] Accept { get; set; }

    [JsonProperty("Connection")]
    public string[] Connection { get; set; }

    [JsonProperty("Content-Length")]
    public string[] ContentLength { get; set; }

    [JsonProperty("Content-Type")]
    public string[] ContentType { get; set; }

    [JsonProperty("User-Agent")]
    public string[] UserAgent { get; set; }

    [JsonProperty("X-Github-Delivery")]
    public Guid[] XGithubDelivery { get; set; }

    [JsonProperty("X-Github-Event")]
    public string[] XGithubEvent { get; set; }

    [JsonProperty("X-Github-Hook-Id")]
    public string[] XGithubHookId { get; set; }

    [JsonProperty("X-Github-Hook-Installation-Target-Id")]
    public string[] XGithubHookInstallationTargetId { get; set; }

    [JsonProperty("X-Github-Hook-Installation-Target-Type")]
    public string[] XGithubHookInstallationTargetType { get; set; }
}
