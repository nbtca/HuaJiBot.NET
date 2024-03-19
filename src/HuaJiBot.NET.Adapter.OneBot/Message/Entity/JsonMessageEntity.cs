using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class JsonMessageEntity(string data) : MessageEntity
{
    public JsonMessageEntity()
        : this("") { }

    [JsonProperty("data")]
    public string Data { get; set; } = data;

    public override JObject ToJson() => new() { ["data"] = Data };
}
