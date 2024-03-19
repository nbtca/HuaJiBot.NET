using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

[Serializable]
internal class ForwardMessageEntity(string name) : MessageEntity
{
    public ForwardMessageEntity()
        : this("") { }

    [JsonProperty("id")]
    public string Name { get; set; } = name;

    public override JObject ToJson() => new() { ["id"] = Name };
}
