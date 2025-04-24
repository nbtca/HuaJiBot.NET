using System;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;

namespace HuaJiBot.NET.Plugin.AIChat.Service.Connector;

public abstract class KernelConnector
{
    protected KernelConnector(BotService service, ModelConfig modelConfig)
    {
        Service = service;
        ModelConfig = modelConfig;
        _kernel = new(() =>
        {
            var builder = CreateKernel();
            builder.AddBotFunctions(service.ExportFunctions);
#if DEBUG
            builder.Services.AddLogging(services =>
                services
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddProvider(new PluginLoggerProvider(Service))
            );
#endif
            var kernel = builder.Build();
            return kernel;
        });
    }

    protected readonly BotService Service;
    protected readonly ModelConfig ModelConfig;
    private readonly Lazy<Kernel> _kernel;
    public Kernel Kernel => _kernel.Value;

    public ChatCompletionAgent CreateChatCompletionAgent(string systemPrompt)
    {
        ChatCompletionAgent agent = new()
        {
            Name = ModelConfig.AgentName,
            Instructions = systemPrompt,
            Kernel = Kernel,
            Arguments = new KernelArguments(GetPromptExecutionSettings()),
#if DEBUG
            LoggerFactory = new LoggerFactory([new PluginLoggerProvider(Service)]),
#endif
        };
        return agent;
    }

    public IChatCompletionService GetChatCompletionService()
    {
        return Kernel.GetRequiredService<IChatCompletionService>();
    }

    protected abstract IKernelBuilder CreateKernel();
    protected abstract PromptExecutionSettings GetPromptExecutionSettings();
}
