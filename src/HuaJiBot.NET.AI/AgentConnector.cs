using HuaJiBot.NET.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace HuaJiBot.NET.AI;

public abstract class AgentConnector
{
    protected AgentConnector(ModelConfig modelConfig)
    {
        ModelConfig = modelConfig;
    }

    protected readonly ModelConfig ModelConfig;

    protected abstract ChatClient CreateChatClient();

    public static AgentConnector Create(IPluginService service, ModelConfig model) =>
        model.Provider switch
        {
            ModelProvider.OpenAI => new OpenAIAgentConnector(service, model),
            ModelProvider.Google => new GoogleAgentConnector(service, model),
            _ => throw new ArgumentOutOfRangeException(nameof(model), model.Provider, null),
        };

    public AIAgent CreateAIAgent(string systemPrompt, AIFunction[]? tools = null)
    {
        return CreateChatClient().AsAIAgent(
            instructions: systemPrompt,
            name: ModelConfig.AgentName,
            tools: tools);
    }

    public AIAgent CreateAIAgentWithOptions(
        string systemPrompt,
        AIFunction[]? functionTools = null,
        AITool[]? mcpTools = null)
    {
        // Combine function tools and MCP tools
        var allTools = new List<AITool>();
        if (functionTools is not null)
            allTools.AddRange(functionTools);
        if (mcpTools is not null)
            allTools.AddRange(mcpTools);

        var options = new ChatClientAgentOptions
        {
            Name = ModelConfig.AgentName,
            ChatOptions = new ChatOptions
            {
                Instructions = systemPrompt,
                Tools = allTools.Count > 0 ? [.. allTools] : null,
            },
        };
        return CreateChatClient().AsAIAgent(options: options);
    }

    public async Task<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var agent = CreateAIAgent("placeholder");
        return await agent.CreateSessionAsync(cancellationToken);
    }
}
