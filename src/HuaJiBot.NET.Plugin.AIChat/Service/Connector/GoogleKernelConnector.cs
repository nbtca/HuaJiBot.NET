using System.Diagnostics.CodeAnalysis;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace HuaJiBot.NET.Plugin.AIChat.Service.Connector;

public class GoogleKernelConnector(BotService service, ModelConfig modelConfig)
    : KernelConnector(service, modelConfig)
{
    [Experimental("SKEXP0070")]
    protected override IKernelBuilder CreateKernel()
    {
        var httpClient = new HttpClient(
            ModelConfig.Logging ? new HttpWithLogHandler(Service) : new HttpClientHandler()
        );
        if (ModelConfig.Endpoint is { Length: > 1 } address)
        {
            httpClient.BaseAddress = new Uri(address);
        }
        var builder = Kernel.CreateBuilder();
        builder.AddGoogleAIGeminiChatCompletion(
            ModelConfig.ModelId,
            ModelConfig.ApiKey,
            httpClient: httpClient
        );
        return builder;
    }

    [Experimental("SKEXP0070")]
    protected override PromptExecutionSettings GetPromptExecutionSettings() =>
        new GeminiPromptExecutionSettings()
        {
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
        };
}
