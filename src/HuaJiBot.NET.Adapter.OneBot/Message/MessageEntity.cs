using System.Runtime.Serialization;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message;

[JsonConverter(typeof(MessageEntityConverter))]
internal abstract class MessageEntity
{
    public abstract JObject ToJson();
}

internal class MessageEntityConverter : JsonConverter<MessageEntity>
{
    public override void WriteJson(
        JsonWriter writer,
        MessageEntity? value,
        JsonSerializer serializer
    )
    {
        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteValue(
            value switch
            {
                AtMessageEntity => "at",
                DiceMessageEntity => "dice",
                FaceMessageEntity => "face",
                ForwardMessageEntity => "forward",
                ImageMessageEntity => "image",
                JsonMessageEntity => "json",
                KeyboardMessageEntity => "keyboard",
                LocationMessageEntity => "location",
                LongMessageEntity => "longmsg",
                MarkdownMessageEntity => "markdown",
                PokeMessageEntity => "poke",
                RecordMessageEntity => "record",
                ReplyMessageEntity => "reply",
                TextMessageEntity => "text",
                VideoMessageEntity => "video",
                _ => throw new NotSupportedException(value?.GetType().Name)
            }
        );
        writer.WritePropertyName("data");
        //serializer.Serialize(writer, value, value.GetType());
        value?.ToJson().WriteTo(writer);
        writer.WriteEndObject();
    }

    private T Create<T>(JObject item, JsonSerializer serializer)
        where T : MessageEntity, new()
    {
        var entity = new T();
        using var reader = item.CreateReader();
        serializer.Populate(reader, entity);
        return entity;
    }

    public override MessageEntity ReadJson(
        JsonReader reader,
        Type objectType,
        MessageEntity? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        var obj = JObject.Load(reader);
        var type = obj.Value<string>("type");
        var data = obj.Value<JObject>("data")!;
        return type switch
        {
            "at" => Create<AtMessageEntity>(data, serializer),
            "dice" => Create<DiceMessageEntity>(data, serializer),
            "face" => Create<FaceMessageEntity>(data, serializer),
            "forward" => Create<ForwardMessageEntity>(data, serializer),
            "image" => Create<ImageMessageEntity>(data, serializer),
            "json" => Create<JsonMessageEntity>(data, serializer),
            "keyboard" => Create<KeyboardMessageEntity>(data, serializer),
            "location" => Create<LocationMessageEntity>(data, serializer),
            "longmsg" => Create<LongMessageEntity>(data, serializer),
            "markdown" => Create<MarkdownMessageEntity>(data, serializer),
            "poke" => Create<PokeMessageEntity>(data, serializer),
            "record" => Create<RecordMessageEntity>(data, serializer),
            "reply" => Create<ReplyMessageEntity>(data, serializer),
            "text" => Create<TextMessageEntity>(data, serializer),
            "video" => Create<VideoMessageEntity>(data, serializer),
            _ => throw new NotSupportedException(type)
        };
    }
}
