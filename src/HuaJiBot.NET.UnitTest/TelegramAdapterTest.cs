using System.Net;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Adapter.Telegram;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Logger;

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
    private sealed class FakeBotApi : HttpMessageHandler
    {
        public List<string> Methods { get; } = [];
        public string? RichMessageBody { get; private set; }
        public bool RejectRichMessage { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var method = request.RequestUri!.Segments[^1];
            Methods.Add(method);
            if (method == "sendRichMessage")
                RichMessageBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var (status, body) = method switch
            {
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
