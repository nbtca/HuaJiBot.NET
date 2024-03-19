using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class MarkdownMessageEntity(string content) : MessageEntity
{
    public MarkdownMessageEntity()
        : this("") { }

    [JsonProperty("content")]
    public string Content { get; set; } = content;

    public override JObject ToJson()
    {
        return new() { ["content"] = Content };
    }
}
