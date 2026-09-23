using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

namespace HuaJiBot.NET.UnitTest;

internal class BroadcastTest
{
    [Test]
    public async Task SendAsync_WithMultipleTargets_BuildsMessagesOnce()
    {
        RecordingAdapter adapter = new();
        var built = 0;

        await Broadcast.SendAsync(
            adapter,
            ["g1", "g2"],
            () =>
            {
                built++;
                return Task.FromResult<SendingMessageBase[]>([new TextMessage("card")]);
            }
        );

        Assert.Multiple(() =>
        {
            Assert.That(built, Is.EqualTo(1));
            Assert.That(adapter.Sends.Select(x => x.Target), Is.EqualTo(new[] { "g1", "g2" }));
            Assert.That(adapter.Sends.Select(x => x.Messages[0]), Is.All.EqualTo(new TextMessage("card")));
        });
    }

    [Test]
    public async Task SendAsync_WithoutTargets_NeverBuildsMessages()
    {
        await Broadcast.SendAsync(
            new RecordingAdapter(),
            [],
            () => throw new InvalidOperationException("messages must not be built")
        );
    }
}
