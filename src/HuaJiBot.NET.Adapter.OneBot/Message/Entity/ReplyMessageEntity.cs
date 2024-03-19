using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class ReplyMessageEntity(uint messageId) : MessageEntity
{
    public ReplyMessageEntity()
        : this(0) { }

    [JsonProperty("id")]
    public string MessageId { get; set; } = messageId.ToString();

    public override JObject ToJson() => new() { ["id"] = MessageId };
}
