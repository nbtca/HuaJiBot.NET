using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Adapter.Satori;

public class Adapter : BotServiceBase
{
    public Adapter(string token) { }

    public override void Reconnect()
    {
        throw new NotImplementedException();
    }

    public override Task SetupServiceAsync()
    {
        throw new NotImplementedException();
    }

    public override string[] GetAllRobots()
    {
        throw new NotImplementedException();
    }

    public override void SendGroupMessage(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string? robotId, string targetGroup, string userId, string text)
    {
        throw new NotImplementedException();
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    public override void Log(object message)
    {
        throw new NotImplementedException();
    }

    public override void Warn(object message)
    {
        throw new NotImplementedException();
    }

    public override void LogDebug(object message)
    {
        throw new NotImplementedException();
    }

    public override void LogError(object message, object detail)
    {
        throw new NotImplementedException();
    }

    public override string GetPluginDataPath()
    {
        throw new NotImplementedException();
    }
}
