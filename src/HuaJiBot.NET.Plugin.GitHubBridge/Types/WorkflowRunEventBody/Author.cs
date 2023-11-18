#nullable disable
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;

internal class Author
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }
}
