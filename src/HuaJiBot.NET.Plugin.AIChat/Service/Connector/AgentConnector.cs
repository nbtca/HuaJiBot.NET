using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace HuaJiBot.NET.Plugin.AIChat.Service.Connector;

public abstract class AgentConnector
{
    protected AgentConnector(ModelConfig modelConfig)
    {
        ModelConfig = modelConfig;
    }

    protected readonly ModelConfig ModelConfig;

    protected abstract ChatClient CreateChatClient();

    public AIAgent CreateAIAgent(string systemPrompt, AIFunction[]? tools = null)
    {
        return CreateChatClient().AsAIAgent(
            instructions: systemPrompt,
            name: ModelConfig.AgentName,
            tools: tools);
    }

    public AIAgent CreateAIAgentWithOptions(
        string systemPrompt,
        AIFunction[]? tools = null,
        ChatOptions? chatOptions = null)
    {
        var options = new ChatClientAgentOptions
        {
            Name = ModelConfig.AgentName,
            ChatOptions = new ChatOptions
            {
                Instructions = systemPrompt,
                Tools = tools,
            },
        };
        if (chatOptions is not null)
        {
            if (chatOptions.Temperature.HasValue)
                options.ChatOptions.Temperature = chatOptions.Temperature;
            if (chatOptions.MaxOutputTokens.HasValue)
                options.ChatOptions.MaxOutputTokens = chatOptions.MaxOutputTokens;
        }
        return CreateChatClient().AsAIAgent(options: options);
    }

    public async Task<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var agent = CreateAIAgent("placeholder");
        return await agent.CreateSessionAsync(cancellationToken);
    }
}
