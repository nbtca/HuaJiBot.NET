using System.ClientModel;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace HuaJiBot.NET.Plugin.AIChat.Service.Connector;

// ReSharper disable once InconsistentNaming
public class OpenAIAgentConnector(BotService service, ModelConfig modelConfig)
    : AgentConnector(modelConfig)
{
    private OpenAIClient? _client;

    protected override ChatClient CreateChatClient() => Client.GetChatClient(ModelConfig.ModelId);

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
                            EnableMessageLogging = ModelConfig.Logging,
                            EnableMessageContentLogging = ModelConfig.Logging,
                            LoggerFactory = LoggerFactory.Create(logger =>
                            {
                                logger
                                    .SetMinimumLevel(LogLevel.Trace)
                                    .AddProvider(new PluginLoggerProvider(service));
                            }),
                        },
                    }
                );
            }
            return _client;
        }
    }
}
