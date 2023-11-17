using System.Threading.Tasks;

namespace HuaJiBot.NET.Bot;

public enum MemberType
{
    Unknown = 0,
    Member = 1,
    Admin = 2,
    Owner = 3
}

public struct SendImageInfo
{
    public string ImagePath { get; set; }
}
public abstract class BotServiceBase
{
    public abstract Task SetupService();
    public Config.ConfigWrapper Config { get; internal set; } = null!;
    public Events.Events Events { get; } = new();
    public abstract string[] GetAllRobots();
    public abstract void SendGroupMessage(string? robotId, string targetGroup, string message);
    public abstract void SendGroupMessage(string? robotId, string targetGroup, SendImageInfo message);
    public abstract void FeedbackAt(
        string? robotId,
        string targetGroup,
        string userId,
        string text
    );
    public abstract MemberType GetMemberType(string robotId, string targetGroup, string userId);
    public abstract string GetNick(string robotId, string userId);
    public abstract void Log(object message);
    public abstract void Warn(object message);
    public abstract void LogDebug(object message);
    public abstract void LogError(object message, object detail);
    public abstract string GetPluginDataPath();
}
