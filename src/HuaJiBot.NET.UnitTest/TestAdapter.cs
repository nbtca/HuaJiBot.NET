using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.UnitTest;

internal class TestAdapter : BotServiceBase
{
    public override ILogger Logger { get; init; } = new ConsoleLogger();

    protected override void ReconnectCore()
    {
        throw new NotImplementedException();
    }

    protected override Task SetupServiceAsyncCore()
    {
        return Task.CompletedTask;
    }

    public override string[] AllRobots => throw new NotImplementedException();

    public override Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        throw new NotImplementedException();
    }

    public override void RecallMessage(string? robotId, string targetGroup, string msgId)
    {
        throw new NotImplementedException();
    }

    protected override void SetGroupNameCore(string? robotId, string targetGroup, string groupName)
    {
        throw new NotImplementedException();
    }

    public override async Task<string[]> FeedbackAt(
        string? robotId,
        string targetGroup,
        string msgId,
        string text
    )
    {
        throw new NotImplementedException();
    }

    protected override MemberType GetMemberTypeCore(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    protected override string GetNickCore(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

}
