using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class ReplyMessageEntity(string messageId) : MessageEntity
{
    public ReplyMessageEntity()
        : this("") { }

    [JsonProperty("id")]
    public string MessageId { get; set; } = messageId;

    public override JObject ToJson() => new() { ["id"] = MessageId };
}
