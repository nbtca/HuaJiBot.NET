using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

namespace HuaJiBot.NET.UnitTest;

internal class BroadcastTest
{
    [Test]
    public async Task SendAsync_SendsSameMessagesToEachTarget()
    {
        RecordingAdapter adapter = new();

        await Broadcast.SendAsync(adapter, ["g1", "g2"], new TextMessage("card"));

        Assert.Multiple(() =>
        {
            Assert.That(adapter.Sends.Select(x => x.Target), Is.EqualTo(new[] { "g1", "g2" }));
            Assert.That(adapter.Sends.Select(x => x.Messages[0]), Is.All.EqualTo(new TextMessage("card")));
        });
    }

    [Test]
    public async Task Cards_RenderEachCardOnce()
    {
        var renders = 0;
        var card = Cards.Once(() => Task.FromResult(++renders));

        await Task.WhenAll(card(), card(), card());

        Assert.That(renders, Is.EqualTo(1));
    }
}
