using HuaJiBot.NET.Adapter.Telegram;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HuaJiBot.NET.UnitTest;

internal class TelegramHtmlTest
{
    [TestCase("a < b & c", "a &lt; b &amp; c")]
    [TestCase("**bold** *it* `x<y`", "<b>bold</b> <i>it</i> <code>x&lt;y</code>")]
    [TestCase("[#22](https://example.com/?a=1&b=2)", "<a href=\"https://example.com/?a=1&amp;b=2\">#22</a>")]
    [TestCase("## Title", "<b>Title</b>")]
    [TestCase("- one\n- two", "• one\n• two")]
    [TestCase("> quoted\n> more", "<blockquote>quoted\nmore</blockquote>")]
    [TestCase("first\n\nsecond", "first\n\nsecond")]
    [TestCase("above\n\n---\n\nbelow", "above\n\nbelow")]
    [TestCase("<b>raw</b>", "&lt;b&gt;raw&lt;/b&gt;")]
    public void Render_ProducesTelegramHtml(string markdown, string html) =>
        Assert.That(TelegramHtml.Render(markdown), Is.EqualTo(html));

    [TestCase("HuaJiBot.NET", "#HuaJiBot_NET")]
    [TestCase("日程", "#日程")]
    [TestCase("mcje-plugins", "#mcje_plugins")]
    public void Tag_KeepsLettersDigitsAndUnderscore(string tag, string expected) =>
        Assert.That(TelegramHtml.Tag(tag), Is.EqualTo(expected));

    [Test]
    public void Compose_TruncatesLongBody()
    {
        var html = TelegramHtml.Compose(new Post(new string('a', 2000), Nothing), 1024);
        Assert.Multiple(() =>
        {
            Assert.That(html, Has.Length.LessThanOrEqualTo(1024));
            Assert.That(html, Does.EndWith("…"));
        });
    }

    [Test]
    public void Compose_DoesNotSplitSurrogatePair()
    {
        var html = TelegramHtml.Compose(new Post(new string('a', 1022) + "😊😊", Nothing), 1024);
        Assert.That(html, Is.EqualTo(new string('a', 1022) + "…"));
    }

    internal static Task<SendingMessageBase[]> Nothing() => Task.FromResult<SendingMessageBase[]>([]);
}

internal class TelegramPostTest
{
    private static TelegramAdapter Adapter(TelegramRichMessageFallbackTest.FakeBotApi api) =>
        new("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", new(api)) { Logger = new ConsoleLogger() };

    [Test]
    public async Task SendGroupMessageAsync_WithTextPost_SendsHtmlMessage()
    {
        TelegramRichMessageFallbackTest.FakeBotApi api = new();

        await Adapter(api)
            .SendGroupMessageAsync(
                null,
                "-100:7",
                new Post("**Steve** joined", TelegramHtmlTest.Nothing)
                {
                    Tags = ["mc", "HuaJiBot.NET"],
                    Links = [new LinkMessage("查看", "https://example.com")],
                    Silent = true,
                }
            );

        var body = api.Bodies.Single();
        Assert.Multiple(() =>
        {
            Assert.That(api.Methods, Is.EqualTo(new[] { "sendMessage" }));
            Assert.That(body, Does.Contain("\"text\":\"#mc #HuaJiBot_NET\\n\\n\\u003Cb\\u003ESteve\\u003C/b\\u003E joined\""));
            Assert.That(body, Does.Contain("\"parse_mode\":\"Html\""));
            Assert.That(body, Does.Contain("\"disable_notification\":true"));
            Assert.That(body, Does.Contain("\"is_disabled\":true"));
            Assert.That(body, Does.Contain("\"message_thread_id\":7"));
            Assert.That(body, Does.Contain("\"url\":\"https://example.com\""));
        });
    }

    [Test]
    public async Task SendGroupMessageAsync_WithImagePost_SendsPhotoWithCaption()
    {
        TelegramRichMessageFallbackTest.FakeBotApi api = new();
        var path = Path.GetTempFileName();
        using (var image = new Image<Rgba32>(1, 1))
            await image.SaveAsPngAsync(path);

        await Adapter(api)
            .SendGroupMessageAsync(
                null,
                "-100",
                new Post("pushed", TelegramHtmlTest.Nothing) { ImagePath = path, Tags = ["push"] }
            );
        File.Delete(path);

        Assert.Multiple(() =>
        {
            Assert.That(api.Methods, Is.EqualTo(new[] { "sendPhoto" }));
            Assert.That(api.Bodies.Single(), Does.Contain("#push\n\npushed"));
        });
    }

    [Test]
    public async Task SendGroupMessageAsync_WhenHtmlIsRejected_ResendsPlainText()
    {
        TelegramRichMessageFallbackTest.FakeBotApi api = new() { RejectHtml = true };

        var ids = await Adapter(api)
            .SendGroupMessageAsync(
                null,
                "-100",
                new Post("**Steve** joined", TelegramHtmlTest.Nothing) { Tags = ["mc"] }
            );

        Assert.Multiple(() =>
        {
            Assert.That(api.Methods, Is.EqualTo(new[] { "sendMessage", "sendMessage" }));
            Assert.That(api.Bodies[1], Does.Not.Contain("parse_mode"));
            Assert.That(api.Bodies[1], Does.Contain("\"text\":\"#mc\\n\\nSteve joined\""));
            Assert.That(ids, Is.EqualTo(new[] { "99" }));
        });
    }

    [Test]
    public async Task RecallMessage_InTopic_UsesGroupId()
    {
        TelegramRichMessageFallbackTest.FakeBotApi api = new();

        Adapter(api).RecallMessage(null, "-100:7", "5");
        for (var i = 0; i < 50 && api.Bodies.Count == 0; i++)
            await Task.Delay(20);

        Assert.That(api.Bodies.Single(), Does.Contain("\"chat_id\":-100"));
    }
}

internal class PostFallbackTest
{
    [Test]
    public async Task ExpandPostsAsync_ReplacesPostWithFallback()
    {
        SendingMessageBase[] messages =
        [
            new ReplyMessage("1"),
            new Post(
                "**new**",
                () => Task.FromResult<SendingMessageBase[]>([new TextMessage("old"), new ImageMessage("a.png")])
            ),
        ];

        Assert.That(
            await messages.ExpandPostsAsync(),
            Is.EqualTo(new SendingMessageBase[] { new ReplyMessage("1"), new TextMessage("old"), new ImageMessage("a.png") })
        );
    }
}
