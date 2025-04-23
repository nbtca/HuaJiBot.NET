using System.Diagnostics.CodeAnalysis;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.SemanticKernel;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

public class GoogleKernelConnector(BotService service, ModelConfig modelConfig)
    : KernelConnector(service, modelConfig)
{
    [Experimental("SKEXP0070")]
    protected override IKernelBuilder CreateKernel()
    {
        var httpClient = new HttpClient(new HttpWithLogHandler(Service, ModelConfig));
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
}
