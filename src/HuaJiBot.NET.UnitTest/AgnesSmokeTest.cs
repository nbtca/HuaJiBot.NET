using HuaJiBot.NET.AI;
using Microsoft.Extensions.AI;

namespace HuaJiBot.NET.UnitTest;

internal class AgnesSmokeTest
{
    [Explicit("Hits the real Agnes API; needs AGNES_API_KEY in the environment")]
    [Test]
    public async Task SummaryCall_RoundTripsThroughOpenAiCompatibleEndpoint()
    {
        var key = Environment.GetEnvironmentVariable("AGNES_API_KEY");
        Assert.That(key, Is.Not.Null, "set AGNES_API_KEY to run this smoke test");

        AgentConnector connector = new OpenAIAgentConnector(
            new TestAdapter(),
            new ModelConfig(
                ModelProvider.OpenAI,
                "https://apihub.agnes-ai.com/v1",
                "agnes-3.0-flash",
                key!
            )
        );
        var session = await connector.CreateSessionAsync();
        var agent = connector.CreateAIAgent("你是一个聊天记录总结助手。使用中文回复。");
        List<ChatMessage> messages =
        [
            new ChatMessage(
                ChatRole.User,
                "以下是 2026-09-21 的群聊记录，请进行总结：\n\n[10:00] 张三: 周六社团活动在 3 号楼\n[10:05] 李四: 收到，我带设备"
            )
        ];

        var response = await agent.RunAsync(messages, session);

        Assert.That(response.Text, Is.Not.Empty);
    }
}
