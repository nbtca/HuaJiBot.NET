using HuaJiBot.NET.Adapter.Kook.Messaging;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.UnitTest;

internal class KookOutboundBuilderTest
{
    private static IReadOnlyList<KookOutboundSegment> Build(params SendingMessageBase[] messages) =>
        KookOutboundBuilder.Build(messages);

    [Test]
    public void TextMentionLink_MergeIntoSingleTextSegment()
    {
        var segments = Build(
            new TextMessage("hello "),
            new AtMessage("123"),
            new LinkMessage("site", "https://example.com")
        );

        Assert.That(segments, Has.Count.EqualTo(1));
        var text = segments[0] as KookTextSegment;
        Assert.That(text, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text!.Markdown, Does.StartWith("hello "));
            // 数字目标渲染为提及（含用户 ID），链接渲染为 KMarkdown 链接。
            Assert.That(text.Markdown, Does.Contain("123"));
            Assert.That(text.Markdown, Does.Contain("https://example.com"));
            Assert.That(text.Quote, Is.Null);
        }
    }

    [Test]
    public void NonNumericMention_FallsBackToSanitizedText()
    {
        var segments = Build(new AtMessage("everyone"));

        Assert.That(segments, Has.Count.EqualTo(1));
        var text = segments[0] as KookTextSegment;
        Assert.That(text, Is.Not.Null);
        // 非 ulong 目标不走 MentionUtils，作为文本转义。
        Assert.That(text!.Markdown, Does.Contain("everyone"));
    }

    [Test]
    public void ImageBetweenTexts_SplitsIntoThreeSegments()
    {
        var segments = Build(
            new TextMessage("before"),
            new ImageMessage("https://img.example/a.png"),
            new TextMessage("after")
        );

        Assert.That(segments, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(segments[0], Is.TypeOf<KookTextSegment>());
            Assert.That(segments[1], Is.TypeOf<KookImageSegment>());
            Assert.That(segments[2], Is.TypeOf<KookTextSegment>());
            Assert.That(((KookImageSegment)segments[1]).ImagePath, Is.EqualTo("https://img.example/a.png"));
        }
    }

    [Test]
    public void ConsecutiveImages_EachBecomeOwnSegment()
    {
        var segments = Build(
            new ImageMessage("https://img.example/a.png"),
            new ImageMessage("https://img.example/b.png")
        );

        Assert.That(segments, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(segments[0], Is.TypeOf<KookImageSegment>());
            Assert.That(segments[1], Is.TypeOf<KookImageSegment>());
        }
    }

    [Test]
    public void SingleImage_ProducesNoEmptyTextSegment()
    {
        var segments = Build(new ImageMessage("https://img.example/a.png"));

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0], Is.TypeOf<KookImageSegment>());
    }

    [Test]
    public void ReplyBeforeText_QuoteLandsOnTheText()
    {
        var segments = Build(
            new ReplyMessage("11111111-1111-1111-1111-111111111111"),
            new TextMessage("reply body")
        );

        Assert.That(segments, Has.Count.EqualTo(1));
        var text = segments[0] as KookTextSegment;
        Assert.That(text, Is.Not.Null);
        Assert.That(text!.Quote, Is.Not.Null);
    }

    [Test]
    public void ReplyBeforeImage_QuoteLandsOnTheImage()
    {
        var segments = Build(
            new ReplyMessage("11111111-1111-1111-1111-111111111111"),
            new ImageMessage("https://img.example/a.png")
        );

        Assert.That(segments, Has.Count.EqualTo(1));
        var image = segments[0] as KookImageSegment;
        Assert.That(image, Is.Not.Null);
        Assert.That(image!.Quote, Is.Not.Null);
    }

    [Test]
    public void ReplyBetweenTexts_MergesIntoOneQuotedTextSegment()
    {
        var segments = Build(
            new TextMessage("first"),
            new ReplyMessage("11111111-1111-1111-1111-111111111111"),
            new TextMessage("second")
        );

        // Reply 不切分文本：前后文本仍合并为一段，整段带引用。
        Assert.That(segments, Has.Count.EqualTo(1));
        var text = segments[0] as KookTextSegment;
        Assert.That(text, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text!.Markdown, Is.EqualTo("firstsecond"));
            Assert.That(text.Quote, Is.Not.Null);
        }
    }

    [Test]
    public void NonGuidReply_ThrowsNotSupported() =>
        // 非法 Guid 使 when 守卫落空，落到 default 分支抛异常。
        Assert.Throws<NotSupportedException>(
            () => Build(new ReplyMessage("not-a-guid"), new TextMessage("body"))
        );

    [Test]
    public void UnsupportedMessageType_Throws() =>
        Assert.Throws<NotSupportedException>(() => Build(new UnsupportedMessage()));

    private sealed record UnsupportedMessage : SendingMessageBase;
}