using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class LongMessageEntity(string name) : MessageEntity
{
    public LongMessageEntity()
        : this("") { }

    [JsonProperty("id")]
    public string Name { get; set; } = name;

    public override JObject ToJson()
    {
        return new() { ["id"] = Name };
    }
}
