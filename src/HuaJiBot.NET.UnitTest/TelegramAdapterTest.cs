using System.Net;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Adapter.Telegram;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Logger;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HuaJiBot.NET.UnitTest;

public class TelegramAdapterTest
{
    [Test]
    public void TelegramAdapter_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var botToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";

        // Act
        var adapter = new TelegramAdapter(botToken)
        {
            Logger = new ConsoleLogger()
        };

        // Assert
        Assert.That(adapter, Is.Not.Null);
        Assert.That(adapter.Logger, Is.Not.Null);
        Assert.That(adapter.AllRobots, Is.Empty); // Should be empty before login
    }

    [Test]
    public void TelegramAdapter_GetPluginDataPath_ShouldReturnValidPath()
    {
        // Arrange
        var botToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
        var adapter = new TelegramAdapter(botToken)
        {
            Logger = new ConsoleLogger()
        };
        var adapterService = (IAdapterService)adapter;

        // Act
        var path = adapterService.GetPluginDataPath();

        // Assert
        Assert.That(path, Is.Not.Null);
        Assert.That(path, Does.Contain("plugins"));
        Assert.That(path, Does.Contain("data"));
        Assert.That(Directory.Exists(path), Is.True);
    }

    [Test]
    public void TelegramAdapter_GetNick_ShouldReturnUserId()
    {
        // Arrange
        var botToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
        var adapter = new TelegramAdapter(botToken)
        {
            Logger = new ConsoleLogger()
        };
        var adapterService = (IAdapterService)adapter;
        var userId = "123456789";

        // Act
        var nick = adapterService.GetNick("botId", userId);

        // Assert
        Assert.That(nick, Is.EqualTo(userId));
    }
    [Test]
    public void ToReplyParameters_WithoutReplyTo_ReturnsNull() =>
        Assert.That(TelegramAdapter.ToReplyParameters(null), Is.Null);

    [Test]
    public void ToReplyParameters_WithNonNumericId_ReturnsNull() =>
        Assert.That(TelegramAdapter.ToReplyParameters("not-a-number"), Is.Null);

    [Test]
    public void ToReplyParameters_WithNumericId_MapsMessageId() =>
        Assert.That(TelegramAdapter.ToReplyParameters("7")?.MessageId, Is.EqualTo(7));
}

public class TelegramRichMessageFallbackTest
{
    internal sealed class FakeBotApi : HttpMessageHandler
    {
        public List<string> Methods { get; } = [];
        public string? RichMessageBody { get; private set; }
        public List<string> Bodies { get; } = [];
        public bool RejectRichMessage { get; init; }
        public bool RejectHtml { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var method = request.RequestUri!.Segments[^1];
            Methods.Add(method);
            var content = await request.Content!.ReadAsStringAsync(cancellationToken);
            Bodies.Add(content);
            if (method == "sendRichMessage")
                RichMessageBody = content;
            var (status, body) = method switch
            {
                _ when RejectHtml && content.Contains("\"parse_mode\":\"Html\"") => (
                    HttpStatusCode.BadRequest,
                    """{"ok":false,"error_code":400,"description":"Bad Request: can't parse entities"}"""
                ),
                "deleteMessage" => (HttpStatusCode.OK, """{"ok":true,"result":true}"""),
                "sendRichMessage" when RejectRichMessage => (
                    HttpStatusCode.BadRequest,
                    """{"ok":false,"error_code":400,"description":"Bad Request: can't parse"}"""
                ),
                _ => (
                    HttpStatusCode.OK,
                    """{"ok":true,"result":{"message_id":99,"date":0,"chat":{"id":-100,"type":"supergroup"}}}"""
                ),
            };
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }

    [Test]
    public async Task SendRichMessageAsync_WhenBotApiRejects_SendsFallback()
    {
        FakeBotApi api = new() { RejectRichMessage = true };
        var adapter = new TelegramAdapter("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", new(api))
        {
            Logger = new ConsoleLogger(),
        };

        var ids = await adapter.SendRichMessageAsync(
            null,
            "-100",
            new RichContent("# Heading"),
            () => Task.FromResult<SendingMessageBase[]>([new TextMessage("plain")])
        );

        Assert.Multiple(() =>
        {
            Assert.That(api.Methods, Is.EqualTo(new[] { "sendRichMessage", "sendMessage" }));
            Assert.That(ids, Is.EqualTo(new[] { "99" }));
        });
    }

    [Test]
    public async Task SendRichMessageAsync_WithoutReplyOrTopic_OmitsNullFields()
    {
        FakeBotApi api = new();
        var adapter = new TelegramAdapter("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", new(api))
        {
            Logger = new ConsoleLogger(),
        };

        await adapter.SendRichMessageAsync(
            null,
            "-100",
            new RichContent("# Heading"),
            () => Task.FromResult<SendingMessageBase[]>([])
        );

        Assert.Multiple(() =>
        {
            Assert.That(api.Methods, Is.EqualTo(new[] { "sendRichMessage" }));
            Assert.That(api.RichMessageBody, Does.Not.Contain("null"));
        });
    }
}

public class TelegramLinkMessageTest
{
    [Test]
    public async Task SendGroupMessageAsync_WithLinks_RendersUrlButtons()
    {
        TelegramRichMessageFallbackTest.FakeBotApi api = new();
        var adapter = new TelegramAdapter("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", new(api))
        {
            Logger = new ConsoleLogger(),
        };

        await adapter.SendGroupMessageAsync(
            null,
            "-100",
            new TextMessage("pushed"),
            new LinkMessage("View changes", "https://s.example/abc")
        );

        var body = api.Bodies.Single();
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("\"inline_keyboard\""));
            Assert.That(body, Does.Contain("\"url\":\"https://s.example/abc\""));
            Assert.That(body, Does.Contain("\"text\":\"View changes\""));
            Assert.That(body, Does.Contain("\"text\":\"pushed\""));
        });
    }
}

public class TelegramPhotoTest
{
    [Test]
    public async Task OpenPhotoAsync_FillsTransparentPixelsWithBlack()
    {
        var path = Path.GetTempFileName();
        using (var source = new Image<Rgba32>(2, 1))
        {
            source[1, 0] = Color.Red;
            await source.SaveAsPngAsync(path);
        }

        await using var photo = await TelegramAdapter.OpenPhotoAsync(path);
        using var result = await Image.LoadAsync<Rgba32>(photo);
        File.Delete(path);

        Assert.Multiple(() =>
        {
            Assert.That(result[0, 0], Is.EqualTo(new Rgba32(0, 0, 0, 255)));
            Assert.That(result[1, 0], Is.EqualTo(Color.Red.ToPixel<Rgba32>()));
        });
    }
}
