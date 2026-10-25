using System.Text.Json;
using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.Bot;

public interface ICommandService
{
    bool ProcessHelp(GroupMessageEventArgs e);
    void ProcessCommand(GroupMessageEventArgs e);
    Task<bool> ProcessCommandAsync(GroupMessageEventArgs e);
    Task ExecuteCommandAsync(string commandId, JsonElement arguments, GroupMessageEventArgs e, CancellationToken cancellationToken = default);
    void SendHelp(GroupMessageEventArgs e);
    IReadOnlyList<CommandDescriptor> GetCommandCatalog();
    void SetupCommands(PluginBase plugin);
    string? ResolveAlias(string alias);
    IReadOnlyDictionary<string, IEnumerable<AgentFunctionInfo>> ExportFunctions { get; }
}
