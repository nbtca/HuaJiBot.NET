using HuaJiBot.NET.Adapter.Kook.Messaging;

namespace HuaJiBot.NET.UnitTest;

internal class KookImageFileNameTest
{
    [TestCase("https://img.example/dir/photo.png", "photo.png")]
    [TestCase("https://img.example/dir/photo.png?x=1&y=2", "photo.png")]
    [TestCase("C:/tmp/local.jpg", "local.jpg")]
    public void ExtractsFileName(string input, string expected) =>
        Assert.That(KookOutboundBuilder.GetImageFileName(input), Is.EqualTo(expected));

    [Test]
    public void UrlWithoutFileName_FallsBackToDefault() =>
        Assert.That(KookOutboundBuilder.GetImageFileName("https://img.example/"), Is.EqualTo("image.png"));
}