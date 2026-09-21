using HuaJiBot.NET.AI;

namespace HuaJiBot.NET.Plugin.AIChat.Config;

public class PluginConfig : ConfigBase
{
    public string SystemPrompt = "你是一个有用的AI助手";
    public ModelConfig Model = new();

    /// <summary>
    /// Groups where the bot answers. Empty answers no group.
    /// </summary>
    public List<string> GroupIds = [];

    /// <summary>
    /// A group's conversation restarts after this many AI replies, which bounds token cost.
    /// </summary>
    public int MaxTurns = 20;

    /// <summary>
    /// MCP (Model Context Protocol) server configurations.
    /// Each entry defines an MCP server that provides tools to the agent.
    /// </summary>
    public List<McpServerConfig> McpServers = [];
}
