using HuaJiBot.NET.Plugin.AIChat.Service;

namespace HuaJiBot.NET.UnitTest;

internal class AsciiArtToolsTest
{
    [Test]
    public void TaagCommandRendersBundledAnsiShadowFont()
    {
        var matched = AsciiArtTools.TryRenderCommand(
            "TAAG处理NBTCA，使用ANSI Shadow，结果直接打印出来",
            out var result
        );

        Assert.That(matched, Is.True);
        Assert.That(result, Is.EqualTo(new AsciiArtTools().RenderAnsiShadow("NBTCA")));
        Assert.That(result.Split('\n'), Has.Length.EqualTo(7));
        Assert.That(result, Does.Contain("█"));
        Assert.That(result, Does.Not.Contain("&#x20;"));
    }

    [Test]
    public void UnsupportedFontDoesNotFallThroughToModel()
    {
        Assert.That(
            AsciiArtTools.TryRenderCommand("TAAG处理NBTCA，使用未知字体", out var result),
            Is.True
        );
        Assert.That(result, Does.Contain("仅支持 ANSI Shadow"));
    }

    [Test]
    public void UnrelatedMessageDoesNotMatch()
    {
        Assert.That(
            AsciiArtTools.TryRenderCommand("请介绍 TAAG 是什么", out _),
            Is.False
        );
    }
}
