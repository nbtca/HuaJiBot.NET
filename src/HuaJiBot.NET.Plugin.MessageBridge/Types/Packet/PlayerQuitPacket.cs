namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class PlayerQuitPacket() : DataPacket<PlayerQuitPacketData>(PacketType.PlayerQuit);

public class PlayerQuitPacketData
{
    public required string PlayerName { get; init; }
}
