#nullable disable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types;

[JsonConverter(typeof(EventJsonConverter))]
internal class Event
{
    [JsonProperty("headers")]
    public Headers Headers { get; set; }

    [JsonProperty("body")]
    public EventBody Body { get; set; }
}

internal class UnknownEventBody : EventBody
{
    public JToken Raw { get; set; }
}

internal abstract class EventBody;
