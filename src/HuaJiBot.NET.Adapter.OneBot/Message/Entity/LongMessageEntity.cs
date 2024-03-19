using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class LongMessageEntity(string name) : MessageEntity
{
    public LongMessageEntity()
        : this("") { }

    [JsonProperty("id")]
    public string Name { get; set; } = name;
}
