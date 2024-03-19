using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class PokeMessageEntity(uint type) : MessageEntity
{
    public PokeMessageEntity()
        : this(0) { }

    [JsonProperty("type")]
    public string Type { get; set; } = type.ToString();

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
}
