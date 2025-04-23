using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.UnitTest;

internal class TestAdapter : BotServiceBase
{
    public override ILogger Logger { get; init; } = new ConsoleLogger();

    public override void Reconnect()
    {
        throw new NotImplementedException();
    }

    public override Task SetupServiceAsync()
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

    public override void SetGroupName(string? robotId, string targetGroup, string groupName)
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

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 取插件数据目录
    /// </summary>
    /// <returns></returns>
    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data")); //插件数据目录，当前目录下的plugins/data
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path); //自动创建目录
        return path;
    }
}
