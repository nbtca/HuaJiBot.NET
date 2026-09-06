using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.UnitTest;

internal class RichMessageTest
{
    private static Task<SendingMessageBase[]> Fallback(params SendingMessageBase[] messages) =>
        Task.FromResult(messages);

    [Test]
    public async Task SendRichMessageAsync_WhenAdapterDoesNotOverride_SendsFallback()
    {
        RecordingAdapter adapter = new();

        var ids = await adapter.SendRichMessageAsync(
            null,
            "group",
            new RichContent("# Heading"),
            () => Fallback(new TextMessage("plain"))
        );

        Assert.Multiple(() =>
        {
            Assert.That(
                adapter.Sends.Single().Messages,
                Is.EqualTo(new SendingMessageBase[] { new TextMessage("plain") })
            );
            Assert.That(ids, Is.EqualTo(new[] { "42" }));
        });
    }

    [Test]
    public async Task SendRichMessageAsync_WithReplyTo_PrependsReplyToFallback()
    {
        RecordingAdapter adapter = new();

        await adapter.SendRichMessageAsync(
            null,
            "group",
            new RichContent("# Heading", ReplyToMessageId: "7"),
            () => Fallback(new TextMessage("plain"))
        );

        Assert.That(
            adapter.Sends.Single().Messages,
            Is.EqualTo(new SendingMessageBase[] { new ReplyMessage("7"), new TextMessage("plain") })
        );
    }

    [Test]
    public async Task SendRichMessageAsync_WhenAdapterOverrides_DoesNotEvaluateFallback()
    {
        var evaluated = false;

        var ids = await new RichAdapter().SendRichMessageAsync(
            null,
            "group",
            new RichContent("# Heading"),
            () =>
            {
                evaluated = true;
                return Fallback();
            }
        );

        Assert.Multiple(() =>
        {
            Assert.That(evaluated, Is.False);
            Assert.That(ids, Is.EqualTo(new[] { "rich" }));
        });
    }
}
