namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class PlayerChatPacket() : DataPacket<PlayerChatPacketData>(PacketType.PlayerChat);

public class PlayerChatPacketData
{
    public required string PlayerName { get; init; }
    public required string Message { get; init; }
}
