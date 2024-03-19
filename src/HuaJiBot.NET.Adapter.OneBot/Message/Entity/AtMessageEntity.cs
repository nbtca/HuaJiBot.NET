using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class AtMessageEntity(uint at) : MessageEntity
{
    public AtMessageEntity()
        : this(0) { }

    [JsonProperty("qq")]
    public string At { get; set; } = at.ToString();

    [JsonIgnore]
    public bool IsAll => At == "all";

    public override JObject ToJson() => new() { ["qq"] = At };
}
