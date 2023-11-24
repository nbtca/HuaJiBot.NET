using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Plugin.MessageBridge.Types;

internal enum MessageType
{
    Chat,
}

[JsonConverter(typeof(ClientMessageConverter))]
internal abstract class ClientMessageBase
{
    internal required MessageType Type { get; set; }
}

internal class ClientMessageConverter : JsonConverter<ClientMessageBase>
{
    public override void WriteJson(
        JsonWriter writer,
        ClientMessageBase? value,
        JsonSerializer serializer
    )
    {
        serializer.Serialize(writer, value);
    }

    public override ClientMessageBase? ReadJson(
        JsonReader reader,
        Type objectType,
        ClientMessageBase? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        var obj = JObject.Load(reader);
        var type = obj["Type"]?.Value<string>();
        return type switch
        {
            nameof(MessageType.Chat) => obj.ToObject<ClientMessage<ChatMessage>>(serializer),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}

internal class ChatMessage
{
    internal required string Sender { get; set; }
    internal required string Message { get; set; }
}

internal class ClientMessage<T> : ClientMessageBase
{
    internal required T Payload { get; set; }
}
