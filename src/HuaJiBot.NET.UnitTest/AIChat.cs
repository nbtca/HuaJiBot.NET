using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using HuaJiBot.NET.Plugin.AIChat.Config;
using HuaJiBot.NET.Plugin.AIChat.Service;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;
using ChatMessage = OpenAI.Chat.ChatMessage;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace HuaJiBot.NET.UnitTest;

internal class AIChat
{
    private OpenAIClient _client;
    private TestAdapter _api = new();

    [SetUp]
    public void Setup()
    {
        _client = new OpenAIClient(
            new ApiKeyCredential("null"),
            new OpenAIClientOptions()
            {
                Endpoint = new Uri("http://localhost:11434/v1"),
                ClientLoggingOptions = new ClientLoggingOptions
                {
                    EnableLogging = true,
                    EnableMessageLogging = true,
                    EnableMessageContentLogging = true,
                    LoggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.AddConsole().SetMinimumLevel(LogLevel.Debug); // 控制台输出最低级别
                    }),
                },
            }
        );
    }

    [Test]
    public async Task GetAllModels()
    {
        var modelClient = _client.GetOpenAIModelClient();
        var models = await modelClient.GetModelsAsync();
        foreach (var openAiModel in models.Value)
        {
            Console.WriteLine(openAiModel.Id);
        }
    }

    [Test]
    public async Task TestChatUsingSemanticKernel()
    {
        KernelConnector connector = new OpenAIKernelConnector(
            _api,
            new ModelConfig(
                ModelProvider.OpenAI,
                ModelId: "huihui_ai/qwen2.5-1m-abliterated:14b",
                Endpoint: "http://localhost:11434/v1",
                AgentName: "Test Bot",
                Logging: true
            )
        );

        var agent = connector.CreateChatCompletionAgent("你是一个有用的人工智能助手。");
        await foreach (
            var response in agent.InvokeAsync(
                new ChatMessageContent[] { new(AuthorRole.User, "今天的日期是？") }
            )
        )
        {
            Console.WriteLine(response.Message);
        }
    }

    [Test]
    public async Task TestChatGeminiUsingSemanticKernel()
    {
        KernelConnector connector = new GoogleKernelConnector(
            _api,
            new ModelConfig(
                ModelProvider.Google,
                ModelId: "gemini-2.0-flash-lite",
                Endpoint: "https://generativelanguage.googleapis.com/",
                ApiKey: "*",
                AgentName: "Test Bot",
                Logging: true
            )
        );

        ChatCompletionAgent agent = connector.CreateChatCompletionAgent(
            "你是一个有用的人工智能助手。"
        );
        await foreach (
            var response in agent.InvokeAsync(
                new ChatMessageContent[] { new(AuthorRole.User, "show me the date") }
            )
        )
        {
            Console.WriteLine(response.Message);
        }
    }

    [Test]
    public async Task TestChat()
    {
        var sw = Stopwatch.StartNew();
        var chatClient = _client.GetChatClient("huihui_ai/qwen2.5-1m-abliterated:14b");
        var response = await chatClient.CompleteChatAsync(
            [
                ChatMessage.CreateSystemMessage("你是一个AI助手"),
                ChatMessage.CreateUserMessage("你好，你是谁？"),
            ]
        );
        foreach (var msg in response.Value.Content)
        {
            Console.WriteLine(msg.Kind);
            Console.WriteLine(msg.Text);
        }
        Console.WriteLine($"耗时：{sw.Elapsed.TotalSeconds}秒");
    }
}
