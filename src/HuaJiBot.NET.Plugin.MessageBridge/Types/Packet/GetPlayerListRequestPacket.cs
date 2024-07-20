namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class GetPlayerListRequestPacket()
    : DataPacket<GetPlayerListRequestPacketData>(PacketType.GetPlayerListRequest);

public class GetPlayerListRequestPacketData
{
    public required string RequestId { get; set; }
}
