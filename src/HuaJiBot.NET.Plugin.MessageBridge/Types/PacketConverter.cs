using HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Plugin.MessageBridge.Types;

internal class PacketConverter : JsonConverter<BasePacket>
{
    public override void WriteJson(JsonWriter writer, BasePacket? value, JsonSerializer serializer)
    {
        serializer.Converters.Remove(this);
        serializer.Serialize(writer, value);
    }

    public override BasePacket? ReadJson(
        JsonReader reader,
        Type objectType,
        BasePacket? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        serializer.Converters.Remove(this);
        var obj = JObject.Load(reader);
        var type = obj["type"]?.ToObject<PacketType>();
        return type switch
        {
            PacketType.PlayerChat => obj.ToObject<PlayerChatPacket>(serializer),
            PacketType.PlayerJoin => obj.ToObject<PlayerJoinPacket>(serializer),
            PacketType.PlayerQuit => obj.ToObject<PlayerQuitPacket>(serializer),
            PacketType.PlayerDeath => obj.ToObject<PlayerDeathPacket>(serializer),
            PacketType.GroupMessage => obj.ToObject<GroupMessagePacket>(serializer),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, obj["type"]?.ToString())
        };
    }
}
