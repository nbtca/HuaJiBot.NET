using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.AIChat.Config;
using Markdig.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

internal abstract class KernelConnector(PluginMain plugin, ModelConfig modelConfig)
{
    private PluginMain _plugin = plugin;
    private ModelConfig _modelConfig = modelConfig;

    public ChatCompletionAgent CreateChatCompletionAgent()
    {
        var kernel = CreateKernel();
        ChatCompletionAgent agent = new()
        {
            Name = "NBTCA-Agent",
            Instructions = "你是一个有用的AI助手。",
            Kernel = kernel,
        };
        return agent;
    }

    protected abstract Kernel CreateKernel();
}

internal class OpenAIKernelConnector(PluginMain plugin, ModelConfig modelConfig)
    : KernelConnector(plugin, modelConfig)
{
    private OpenAIClient? _client;

    public ChatCompletionAgent CreateChatCompletionAgent()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(_modelConfig.ModelId, Client);
        var kernel = builder.Build();
        ChatCompletionAgent agent = new()
        {
            Name = "NBTCA-Agent",
            Instructions = "你是一个有用的AI助手。",
            Kernel = kernel,
        };
        return agent;
    }

    private OpenAIClient Client
    {
        get
        {
            if (_client is null //首次获取
            //TODO : || _modelConfig //模型设置有变动自动重新加载
            )
            {
                _client = new OpenAIClient(
                    new ApiKeyCredential(
                        string.IsNullOrEmpty(_modelConfig.ApiKey) ? "null" : _modelConfig.ApiKey
                    ),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(_modelConfig.Endpoint),
                        ClientLoggingOptions = new()
                        {
                            EnableLogging = _modelConfig.Logging,
                            LoggerFactory = LoggerFactory.Create(logger =>
                            {
                                logger.AddProvider(new PluginLoggerProvider(_plugin));
                            }),
                        },
                    }
                );
            }
            return _client;
        }
    }
}
