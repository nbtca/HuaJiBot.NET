namespace HuaJiBot.NET;

public enum MemberType
{
    Unknown = 0,
    Member = 1,
    Admin = 2,
    Owner = 3
}

public abstract class BotServiceBase
{
    public abstract string[] GetAllRobots();
    public abstract void SendGroupMessage(string robotId, string targetGroup, string message);
    public abstract string GetGroupName(string robotId, string targetGroup);
    public abstract string GetMemberCard(string robotId, string targetGroup, string id);
    public abstract void FeedbackAt(string robotId, string targetGroup, string id, string text);
    public abstract MemberType GetMemberType(string robotId, string targetGroup, string id);
    public abstract string GetNick(string robotId, string id);
    public abstract void Log(object message);
    public abstract void LogDebug(object message);
    public abstract void LogError(object message, object detail);
    public abstract string GetPluginDataPath();
}
