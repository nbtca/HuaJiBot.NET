using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class MarkdownMessageEntity(string content) : MessageEntity
{
    public MarkdownMessageEntity()
        : this("") { }

    [JsonProperty("content")]
    public string Content { get; set; } = content;
}
