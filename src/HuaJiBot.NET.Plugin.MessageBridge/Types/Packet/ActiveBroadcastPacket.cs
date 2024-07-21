using System.Net.Http.Headers;

namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class ActiveBroadcastPacket()
    : DataPacket<ActiveBroadcastPacketData>(PacketType.ActiveClientsChange);

public class ActiveBroadcastPacketData
{
    public required ClientInfo[] Clients;

    public class ClientInfo
    {
        public required string Address;
        public required Dictionary<string, string[]> Headers;
    }
}
