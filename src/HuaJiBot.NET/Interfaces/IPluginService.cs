using System.Runtime.CompilerServices;
using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Config;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Interfaces;

public interface IPluginService
{
    void Log(
        object message,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    );
    void Warn(
        object message,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    );
    void LogDebug(
        object message,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    );
    void LogError(
        object message,
        object? detail,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    );
    IEvents Events { get; }
    Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    );
    void RecallMessage(string? robotId, string targetGroup, string msgId);
    Task<string[]> FeedbackAt(string? robotId, string targetGroup, string msgId, string text);
    string[] AllRobots { get; }
    ConfigWrapper Config { get; }
    ILogger Logger { get; }
    IReadOnlyDictionary<string, IEnumerable<AgentFunctionInfo>> ExportFunctions { get; }
    string GetPluginDataPath();
}
