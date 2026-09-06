using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

namespace HuaJiBot.NET.UnitTest;

internal class BroadcastTest
{
    private static readonly RichContent Content = new("## repo");

    [Test]
    public async Task SendAsync_WhenServiceHandlesRich_NeverEvaluatesFallback()
    {
        RichAdapter adapter = new();

        await Broadcast.SendAsync(
            adapter,
            ["g1", "g2"],
            Content,
            () => throw new InvalidOperationException("fallback must not be built")
        );

        Assert.That(adapter.Targets, Is.EqualTo(new[] { "g1", "g2" }));
    }

    [Test]
    public async Task SendAsync_WithMultipleTargets_EvaluatesFallbackOnce()
    {
        RecordingAdapter adapter = new();
        var built = 0;

        await Broadcast.SendAsync(
            adapter,
            ["g1", "g2"],
            Content,
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
        });
    }
}
