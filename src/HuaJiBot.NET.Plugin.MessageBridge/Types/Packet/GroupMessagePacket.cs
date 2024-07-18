namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class GroupMessagePacket()
    : DataPacket<GroupMessagePacketData>(PacketType.GroupMessage);

public class GroupMessagePacketData
{
    public required string GroupId { get; init; }
    public required string GroupName { get; init; }
    public required string SenderId { get; init; }
    public required string SenderName { get; init; }
    public required string Message { get; init; }
}
