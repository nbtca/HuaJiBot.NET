using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class FaceMessageEntity(int id) : MessageEntity
{
    public FaceMessageEntity()
        : this(0) { }

    [JsonProperty("id")]
    public string Id { get; set; } = id.ToString();

    public override JObject ToJson() => new() { ["id"] = Id };
}
