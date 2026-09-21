using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class UnknownMessageEntity(string type, JObject data) : MessageEntity
{
    public string Type { get; } = type;

    public override JObject ToJson() => data;
}
