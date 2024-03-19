using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class TextMessageEntity(string text) : MessageEntity
{
    public TextMessageEntity()
        : this("") { }

    [JsonProperty("text")]
    public string Text { get; set; } = text;
}
