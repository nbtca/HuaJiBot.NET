using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HuaJiBot.NET.Plugin.FlueBridge;

namespace HuaJiBot.NET.UnitTest;

public partial class FlueBridgeTest
{
    private static string ProtocolFixture(string name) =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "AgentProtocol", name));

    [Test]
    public void SharedProtocolExamplesRoundTripThroughActualBridgeRecords()
    {
        var ingressJson = ProtocolFixture("examples/ingress.json");
        var ingress = JsonSerializer.Deserialize<IngressEvent>(ingressJson, BridgeJson.Options)!;
        var replyJson = ProtocolFixture("examples/reply-job.json");
        var reply = JsonSerializer.Deserialize<ReplyJob>(replyJson, BridgeJson.Options)!;
        var commandJson = ProtocolFixture("v2/examples/command-job.json");
        var command = JsonSerializer.Deserialize<CommandJob>(commandJson, BridgeJson.Options)!;
        Assert.Multiple(() =>
        {
            Assert.That(JsonNode.DeepEquals(JsonNode.Parse(ingressJson), JsonSerializer.SerializeToNode(ingress, BridgeJson.Options)), Is.True);
            Assert.That(JsonNode.DeepEquals(JsonNode.Parse(replyJson), JsonSerializer.SerializeToNode(reply, BridgeJson.Options)), Is.True);
            Assert.That(JsonNode.DeepEquals(JsonNode.Parse(commandJson), JsonSerializer.SerializeToNode(command, BridgeJson.Options)), Is.True);
            Assert.That(ingress.Kind, Is.EqualTo("chat"));
            Assert.That(reply.Destination.ReplyToMessageId, Is.EqualTo(ingress.Message.MessageId));
            Assert.That(command.Command.Id, Is.EqualTo("schedule"));
        });
    }

    [Test]
    public void SharedProtocolVectorsMatchRawBodySignatureAndIdentifierEncoding()
    {
        using var vectors = JsonDocument.Parse(ProtocolFixture("test-vectors.json"));
        foreach (var vector in vectors.RootElement.GetProperty("hmac").EnumerateArray())
        {
            var actual = "v1=" + IngressClient.Sign(vector.GetProperty("secret").GetString()!,
                vector.GetProperty("timestamp").GetString()!, Encoding.UTF8.GetBytes(vector.GetProperty("body").GetString()!));
            Assert.That(actual, Is.EqualTo(vector.GetProperty("signature").GetString()));
        }
        foreach (var vector in vectors.RootElement.GetProperty("identifiers").EnumerateArray())
        {
            Assert.That(Uri.EscapeDataString(vector.GetProperty("raw").GetString()!), Is.EqualTo(vector.GetProperty("encoded").GetString()));
        }
    }
}
