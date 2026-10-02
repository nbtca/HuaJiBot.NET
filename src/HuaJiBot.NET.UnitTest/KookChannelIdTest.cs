using HuaJiBot.NET.Adapter.Kook.Channels;

namespace HuaJiBot.NET.UnitTest;

internal class KookChannelIdTest
{
    [Test]
    public void ParsesDecimalAsGuildChannel()
    {
        Assert.That(KookChannelId.TryParse("2810246202", out var id), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(id.Kind, Is.EqualTo(KookChannelKind.Guild));
            Assert.That(id.GuildChannelId, Is.EqualTo(2810246202UL));
        });
    }

    [Test]
    public void ParsesGuidAsDirectChannel()
    {
        var chatCode = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";
        Assert.That(KookChannelId.TryParse(chatCode, out var id), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(id.Kind, Is.EqualTo(KookChannelKind.Direct));
            Assert.That(id.DirectChatCode, Is.EqualTo(Guid.Parse(chatCode)));
        });
    }

    [TestCase("")]
    [TestCase("not-an-id")]
    [TestCase(null)]
    public void RejectsInvalidIdentifier(string? raw) =>
        Assert.That(KookChannelId.TryParse(raw, out _), Is.False);

    [Test]
    public void GuildFactory_SetsKindAndValue()
    {
        var id = KookChannelId.Guild(42);
        Assert.Multiple(() =>
        {
            Assert.That(id.Kind, Is.EqualTo(KookChannelKind.Guild));
            Assert.That(id.GuildChannelId, Is.EqualTo(42UL));
        });
    }

    [Test]
    public void DirectFactory_SetsKindAndValue()
    {
        var code = Guid.NewGuid();
        var id = KookChannelId.Direct(code);
        Assert.Multiple(() =>
        {
            Assert.That(id.Kind, Is.EqualTo(KookChannelKind.Direct));
            Assert.That(id.DirectChatCode, Is.EqualTo(code));
        });
    }
}