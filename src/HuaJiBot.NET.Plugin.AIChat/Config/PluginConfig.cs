namespace HuaJiBot.NET.Plugin.AIChat.Config;

public class PluginConfig : ConfigBase
{
    public string SystemPrompt = "你是一个有用的AI助手";
    public ModelConfig Model = new();

    /// <summary>
    /// MCP (Model Context Protocol) server configurations.
    /// Each entry defines an MCP server that provides tools to the agent.
    /// </summary>
    public List<McpServerConfig> McpServers = [];
}
