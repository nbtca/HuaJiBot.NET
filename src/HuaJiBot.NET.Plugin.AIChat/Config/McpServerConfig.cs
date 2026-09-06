namespace HuaJiBot.NET.Plugin.AIChat.Config;

/// <summary>
/// Configuration for an MCP (Model Context Protocol) server connection.
/// </summary>
public record McpServerConfig(
    /// <summary>
    /// Unique identifier for this MCP server instance.
    /// </summary>
    string Name = "",

    /// <summary>
    /// The type of MCP transport: "stdio" for local process, "sse" for HTTP/SSE.
    /// </summary>
    McpTransportType TransportType = McpTransportType.Stdio,

    /// <summary>
    /// For stdio: the command to execute (e.g., "npx").
    /// For SSE: the server URL (e.g., "https://example.com/mcp").
    /// </summary>
    string Command = "",

    /// <summary>
    /// Whether this MCP server is enabled.
    /// </summary>
    bool Enabled = true,

    /// <summary>
    /// For stdio: arguments passed to the command.
    /// </summary>
    string[]? Args = null,

    /// <summary>
    /// Environment variables to set for stdio transport.
    /// </summary>
    Dictionary<string, string>? Env = null
);

/// <summary>
/// Transport type for MCP server connections.
/// </summary>
public enum McpTransportType
{
    /// <summary>
    /// Local process communication via stdin/stdout.
    /// </summary>
    Stdio,

    /// <summary>
    /// HTTP with Server-Sent Events.
    /// </summary>
    Sse,
}
