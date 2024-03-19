using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class ReplyMessageEntity(uint messageId) : MessageEntity
{
    public ReplyMessageEntity()
        : this(0) { }

    [JsonProperty("id")]
    public string MessageId { get; set; } = messageId.ToString();
}
