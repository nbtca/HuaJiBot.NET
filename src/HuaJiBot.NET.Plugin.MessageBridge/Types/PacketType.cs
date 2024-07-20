using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace HuaJiBot.NET.Plugin.MessageBridge.Types;

[JsonConverter(typeof(StringEnumConverter), typeof(SnakeCaseNamingStrategy))]
public enum PacketType
{
    PlayerChat,
    PlayerJoin,
    PlayerQuit,
    PlayerDeath,
    GroupMessage,
    GetPlayerListRequest,
    GetPlayerListResponse
}
