using HuaJiBot.NET.Plugin.MessageBridge.Types;
using HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

namespace HuaJiBot.NET.UnitTest;

internal class MessageTest
{
    [Test]
    public void TestPlayerDeathPacket()
    {
        var pkt = new PlayerDeathPacket
        {
            Source = new SenderInformation("test Server", "Test", "1.20.4"),
            Data = new PlayerDeathPacketData { PlayerName = "Test1", DeathMessage = "Test2" }
        };
        Assert.That(pkt.Type, Is.EqualTo(PacketType.PlayerDeath));
        Console.WriteLine(pkt.ToJson());
        Console.WriteLine(pkt.ToJson());
        Console.WriteLine(BasePacket.FromJson(pkt.ToJson())?.ToJson());
    }
}
