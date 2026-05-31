using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using HuaJiBot.NET.Plugin.AIChat.Config;
using HuaJiBot.NET.Plugin.AIChat.Service.Connector;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;
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

    [Explicit("Connects to local AI service")]
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

    [Explicit("Connects to local AI service")]
    [Test]
    public async Task TestChatUsingSemanticKernel()
    {
        AgentConnector connector = new OpenAIAgentConnector(
            _api,
            new ModelConfig(
                ModelProvider.OpenAI,
                ModelId: "huihui_ai/qwen2.5-1m-abliterated:14b",
                Endpoint: "http://localhost:11434/v1",
                AgentName: "Test Bot",
                Logging: true
            )
        );
        var agent = connector.CreateAIAgent("你是一个有用的人工智能助手。");
        var session = await connector.CreateSessionAsync();
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>
        {
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "现在的日期和时间？")
        };
        await foreach (var update in agent.RunStreamingAsync(messages, session))
        {
            if (!string.IsNullOrEmpty(update.Text))
                Console.WriteLine(update.Text);
        }
    }

    [Explicit("Connects to external AI service")]
    [Test]
    public async Task TestChatGeminiUsingSemanticKernel()
    {
        AgentConnector connector = new GoogleAgentConnector(
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
        var agent = connector.CreateAIAgent("你是一个有用的人工智能助手。");
        var session = await connector.CreateSessionAsync();
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>
        {
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "现在的日期和时间")
        };
        await foreach (var update in agent.RunStreamingAsync(messages, session))
        {
            if (!string.IsNullOrEmpty(update.Text))
                Console.WriteLine(update.Text);
        }
    }

    [Explicit("Connects to local AI service")]
    [Test]
    public async Task TestChat()
    {
        var sw = Stopwatch.StartNew();
        var chatClient = _client.GetChatClient("huihui_ai/qwen2.5-1m-abliterated:14b");
        var response = await chatClient.CompleteChatAsync(
            [
                OpenAIChatMessage.CreateSystemMessage("你是一个AI助手"),
                OpenAIChatMessage.CreateUserMessage("你好，你是谁？"),
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
