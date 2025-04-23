using System.ClientModel;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using OpenAI;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

// ReSharper disable once InconsistentNaming
public class OpenAIKernelConnector(BotService service, ModelConfig modelConfig)
    : KernelConnector(service, modelConfig)
{
    private OpenAIClient? _client;

    protected override IKernelBuilder CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(ModelConfig.ModelId, Client);
        return builder;
    }

    private OpenAIClient Client
    {
        get
        {
            if (_client is null)
            {
                _client = new OpenAIClient(
                    new ApiKeyCredential(
                        string.IsNullOrEmpty(ModelConfig.ApiKey) ? "null" : ModelConfig.ApiKey
                    ),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(ModelConfig.Endpoint),
                        ClientLoggingOptions = new()
                        {
                            EnableLogging = ModelConfig.Logging,
                            LoggerFactory = LoggerFactory.Create(logger =>
                            {
                                logger.AddProvider(new PluginLoggerProvider(Service));
                            }),
                        },
                    }
                );
            }
            return _client;
        }
    }
}
