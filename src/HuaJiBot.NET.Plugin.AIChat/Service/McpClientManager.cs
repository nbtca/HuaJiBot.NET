using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

/// <summary>
/// Manages MCP (Model Context Protocol) client connections and provides tools to agents.
/// </summary>
public sealed class McpClientManager : IAsyncDisposable
{
    private readonly List<IMcpClient> _clients = [];
    private readonly List<AITool> _tools = [];
    private readonly ILogger? _logger;
    private bool _initialized;

    public McpClientManager(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all MCP tools from connected servers.
    /// </summary>
    public IReadOnlyList<AITool> Tools => _tools.AsReadOnly();

    /// <summary>
    /// Initializes MCP clients based on the provided configurations.
    /// </summary>
    public async Task InitializeAsync(IEnumerable<McpServerConfig> configs, CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        foreach (var config in configs)
        {
            if (!config.Enabled || string.IsNullOrEmpty(config.Name))
                continue;

            try
            {
                var client = await CreateClientAsync(config, cancellationToken);
                if (client is not null)
                {
                    _clients.Add(client);

                    // List tools from this MCP server
                    var serverTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                    _tools.AddRange(serverTools);

                    _logger?.LogInformation("Connected to MCP server '{Name}' with {ToolCount} tools",
                        config.Name, serverTools.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to connect to MCP server '{Name}'", config.Name);
            }
        }

        _initialized = true;
    }

    private async Task<IMcpClient?> CreateClientAsync(McpServerConfig config, CancellationToken cancellationToken)
    {
        var clientTransport = config.TransportType switch
        {
            McpTransportType.Stdio => CreateStdioTransport(config),
            McpTransportType.Sse => CreateSseTransport(config),
            _ => throw new NotSupportedException($"MCP transport type '{config.TransportType}' is not supported."),
        };

        return await McpClientFactory.CreateAsync(clientTransport, cancellationToken: cancellationToken);
    }

    private static IClientTransport CreateStdioTransport(McpServerConfig config)
    {
        var args = config.Args ?? [];

        return new StdioClientTransport(new()
        {
            Command = config.Command,
            Arguments = args,
            Name = config.Name,
            EnvironmentVariables = config.Env as Dictionary<string, string?>,
        });
    }

    private static IClientTransport CreateSseTransport(McpServerConfig config)
    {
        return new SseClientTransport(new()
        {
            Endpoint = new Uri(config.Command),
            Name = config.Name,
        });
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            try
            {
                await client.DisposeAsync();
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _clients.Clear();
        _tools.Clear();
    }
}
