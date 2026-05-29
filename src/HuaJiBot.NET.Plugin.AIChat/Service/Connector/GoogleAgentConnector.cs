using System.ClientModel;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.AIChat.Config;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace HuaJiBot.NET.Plugin.AIChat.Service.Connector;

public class GoogleAgentConnector(BotService service, ModelConfig modelConfig)
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
                var endpoint = string.IsNullOrEmpty(ModelConfig.Endpoint)
                    ? "https://generativelanguage.googleapis.com/v1beta/openai/"
                    : ModelConfig.Endpoint;
                _client = new OpenAIClient(
                    new ApiKeyCredential(
                        string.IsNullOrEmpty(ModelConfig.ApiKey) ? "null" : ModelConfig.ApiKey
                    ),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(endpoint),
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
