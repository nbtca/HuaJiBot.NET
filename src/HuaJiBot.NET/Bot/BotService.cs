using System.Runtime.CompilerServices;
using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Bot;

public enum MemberType
{
    Unknown = 0,
    Member = 1,
    Admin = 2,
    Owner = 3,
}

public abstract record SendingMessageBase
{
    public static implicit operator SendingMessageBase(string text) => new TextMessage(text);
};

public sealed record TextMessage(string Text) : SendingMessageBase;

public sealed record ImageMessage(string ImagePath) : SendingMessageBase;

public sealed record AtMessage(string Target) : SendingMessageBase;

public sealed record ReplyMessage(string MessageId) : SendingMessageBase;

public abstract class BotServiceBase : BotService
{
    public override EventsSender Events { get; } = new();
}

public abstract class BotService : IMessageService
{
    #region Logger
    public abstract ILogger Logger { get; init; }

    public void Log(
        object message,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Logger.Log(AppendSourceInfo(message, file, line));

    public void Warn(
        object message,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Logger.Warn(AppendSourceInfo(message, file, line));

    public void LogDebug(
        object message,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Logger.LogDebug(AppendSourceInfo(message, file, line));

    public void LogError(
        object message,
        object? detail,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Logger.LogError(AppendSourceInfo(message, file, line), detail);
    #endregion

    private string AppendSourceInfo(object msg, string? file = null, int? line = null)
    {
        const string indexStr = "huaji-bot-dotnet";
        if (file?.IndexOf(indexStr) is > 0 and var index)
        {
            file = file[(index + indexStr.Length)..];
        }
        return $"{msg} [{file}:{line}]";
    }

    public abstract void Reconnect();
    public abstract Task SetupServiceAsync();
    public Config.ConfigWrapper Config { get; internal set; } = null!;
    public abstract IEvents Events { get; }
    public abstract string[] AllRobots { get; }

    public abstract Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    );
    public abstract void RecallMessage(string? robotId, string targetGroup, string msgId);
    public abstract void SetGroupName(string? robotId, string targetGroup, string groupName);

    public virtual async Task<string[]> FeedbackAt(
        string? robotId,
        string targetGroup,
        string msgId,
        string text
    )
    {
        return await SendGroupMessageAsync(
            robotId,
            targetGroup,
            new ReplyMessage(msgId),
            new TextMessage(text)
        );
    }

    public abstract MemberType GetMemberType(string robotId, string targetGroup, string userId);
    public abstract string GetNick(string robotId, string userId);

    public virtual string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data"));
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    // Command service composition
    private readonly ICommandService _commandService;

    protected BotService()
    {
        _commandService = new CommandService(this);
    }

    public IReadOnlyDictionary<string, IEnumerable<AgentFunctionInfo>> ExportFunctions =>
        _commandService.ExportFunctions;

    public void SetupCommands(PluginBase plugin) => _commandService.SetupCommands(plugin);
}
