namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class PlayerDeathPacket() : DataPacket<PlayerDeathPacketData>(PacketType.PlayerDeath);

public class PlayerDeathPacketData
{
    public required string PlayerName { get; init; }
    public required string DeathMessage { get; init; }
}
