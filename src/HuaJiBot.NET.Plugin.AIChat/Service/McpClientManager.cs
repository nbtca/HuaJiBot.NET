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
    private readonly Dictionary<string, IMcpClient> _toolClients = new(StringComparer.Ordinal);
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
                    foreach (var tool in serverTools)
                        _toolClients.TryAdd(tool.Name, client);

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

    public async Task<string?> CallTextToolAsync(
        string name,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!_toolClients.TryGetValue(name, out var client))
            return null;
        var result = await client.CallToolAsync(name, arguments, cancellationToken: cancellationToken);
        if (result.IsError == true)
            throw new InvalidOperationException($"MCP tool '{name}' failed.");
        return string.Join("\n", result.Content.Where(item => item.Type == "text").Select(item => item.Text));
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
            EnvironmentVariables = config.Env?.ToDictionary(
                entry => entry.Key,
                entry => (string?)entry.Value
            ),
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
        _toolClients.Clear();
    }
}
