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
        JsonSerializer s
    )
    {
        s.Converters.Remove(this);
        var o = JObject.Load(reader);
        var type = o["type"]?.ToObject<PacketType>();
        return type switch
        {
            PacketType.PlayerChat => o.ToObject<PlayerChatPacket>(s),
            PacketType.PlayerJoin => o.ToObject<PlayerJoinPacket>(s),
            PacketType.PlayerQuit => o.ToObject<PlayerQuitPacket>(s),
            PacketType.PlayerDeath => o.ToObject<PlayerDeathPacket>(s),
            PacketType.GroupMessage => o.ToObject<GroupMessagePacket>(s),
            PacketType.GetPlayerListRequest => o.ToObject<GetPlayerListRequestPacket>(s),
            PacketType.GetPlayerListResponse => o.ToObject<GetPlayerListResponsePacket>(s),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, o["type"]?.ToString())
        };
    }
}
