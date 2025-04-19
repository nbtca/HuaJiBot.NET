namespace HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

public sealed class PlayerAchievementPacket()
    : DataPacket<PlayerAchievementPacketData>(PacketType.PlayerAchievement);

public class PlayerAchievementPacketData
{
    public required string PlayerName { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string[] Criteria { get; init; }
}
