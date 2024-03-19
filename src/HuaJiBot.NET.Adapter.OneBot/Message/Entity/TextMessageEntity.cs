using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class TextMessageEntity(string text) : MessageEntity
{
    public TextMessageEntity()
        : this("") { }

    [JsonProperty("text")]
    public string Text { get; set; } = text;

    public override JObject ToJson() => new() { ["text"] = Text };
}
