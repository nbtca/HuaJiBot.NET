namespace HuaJiBot.NET.MQ;

public class ActiveBroadcastPacketData
{
    public required ClientInfo[] Clients;

    public class ClientInfo
    {
        public required string Address;
        public required Dictionary<string, string[]> Headers;
    }
}
