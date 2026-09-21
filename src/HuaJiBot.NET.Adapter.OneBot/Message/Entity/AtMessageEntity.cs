using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class AtMessageEntity(string at) : MessageEntity
{
    public AtMessageEntity()
        : this("") { }

    [JsonProperty("qq")]
    public string At { get; set; } = at;

    [JsonIgnore]
    public bool IsAll => At == "all";

    public override JObject ToJson() => new() { ["qq"] = At };
}
