namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class PlayerJoinPacket() : DataPacket<PlayerJoinPacketData>(PacketType.PlayerJoin);

public class PlayerJoinPacketData
{
    public required string PlayerName { get; init; }
}
