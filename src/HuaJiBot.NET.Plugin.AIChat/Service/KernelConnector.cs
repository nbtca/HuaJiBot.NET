using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.AIChat.Config;
using HuaJiBot.NET.Plugin.AIChat.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

public abstract class KernelConnector(BotService service, ModelConfig modelConfig)
{
    protected readonly BotService Service = service;
    protected readonly ModelConfig ModelConfig = modelConfig;

    private void EnableBotFunctions(IKernelBuilder builder)
    {
        builder.Plugins.AddFromType<BasicPlugin>();
    }

    public ChatCompletionAgent CreateChatCompletionAgent()
    {
        var builder = CreateKernel();
#if DEBUG
        builder.Services.AddLogging(services =>
            services.AddProvider(new PluginLoggerProvider(Service))
        );
#endif
        var kernel = builder.Build();
        ChatCompletionAgent agent = new()
        {
            Name = ModelConfig.AgentName,
            Instructions = ModelConfig.ModelId,
            Kernel = kernel,
        };
        return agent;
    }

    protected abstract IKernelBuilder CreateKernel();
}
