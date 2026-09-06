using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.Bot;

public interface ICommandService
{
    bool ProcessHelp(GroupMessageEventArgs e);
    void ProcessCommand(GroupMessageEventArgs e);
    void SetupCommands(PluginBase plugin);
    IReadOnlyDictionary<string, IEnumerable<AgentFunctionInfo>> ExportFunctions { get; }
}
