#nullable disable
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types.IssuesEventBody;

internal class Label
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }
}
