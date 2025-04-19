namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class GetPlayerListResponsePacket()
    : DataPacket<GetPlayerListResponsePacketData>(PacketType.GetPlayerListResponse);

public class GetPlayerListResponsePacketData
{
    public required string RequestId { get; set; }

    public required PlayerInfo[] Players { get; set; }

    public record PlayerInfo(string Name, string Uuid, int Ping, int[] Position, string World);
}
