using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using HuaJiBot.NET.Adapter.OneBot;
using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Plugin.AIChat.Service;
using LiteDB;
using Microsoft.Extensions.AI;
using Newtonsoft.Json.Linq;
using OpenAI;

namespace HuaJiBot.NET.UnitTest;

internal class VisionInputTest
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jG1sAAAAASUVORK5CYII=");

    [Test]
    public void OneBotAttachments_DoNotTreatTypedUrlsAsImages()
    {
        var messages = JArray.Parse("""
            [{"type":"at","data":{"qq":"123"}},
             {"type":"text","data":{"text":"https://example.com/typed.png"}},
             {"type":"image","data":{"file":"qq.image","url":"https://example.com/actual.png"}},
             {"type":"image","data":{"file":"https://example.com/fallback.png"}}]
            """).ToObject<List<MessageEntity>>()!;
        Assert.That(OneBotMessageHandler.ExtractImageUrls(messages),
            Is.EqualTo(new[] { "https://example.com/actual.png", "https://example.com/fallback.png" }));
        var reader = new OneBotCommandReader(new TestAdapter(), messages);
        Assert.That(reader.At(out var target), Is.True);
        Assert.That(target, Is.EqualTo("123"));
        Assert.That(reader.Input(out var text, true), Is.True);
        Assert.That(text, Is.EqualTo("https://example.com/typed.png"));
    }

    [Test]
    public async Task ImageOnly_DefaultPromptAndRealImageContent()
    {
        using var http = new HttpClient(new ResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new ByteArrayContent(Png) }));
        var message = VisionInput.CreateMessage(null, ["https://example.com/a.png"]);
        await new VisionInput(http).PrepareAsync([message], true, 4, 1024, default);
        Assert.That(message.Text, Does.Contain("图片"));
        Assert.That(message.Contents.OfType<DataContent>().Single().MediaType, Is.EqualTo("image/png"));
        Assert.That(message.Contents.OfType<DataContent>().Single().Data.ToArray(), Is.EqualTo(Png));
    }

    [Test]
    public void VisionDisabled_RejectsBeforeDownloading()
    {
        var message = VisionInput.CreateMessage("看图", ["https://example.com/a.png"]);
        var error = Assert.ThrowsAsync<VisionInputException>(() =>
            new VisionInput().PrepareAsync([message], false, 4, 1024, default));
        Assert.That(error!.Message, Does.Contain("未启用"));
    }

    [Test]
    public void ImageLimitIncludesHistory()
    {
        var history = VisionInput.CreateMessage("之前的图", ["https://example.com/a.png"]);
        var current = VisionInput.CreateMessage("新的图", ["https://example.com/b.png"]);
        Assert.ThrowsAsync<VisionInputException>(() =>
            new VisionInput().PrepareAsync([history, current], true, 1, 1024, default));
    }

    [TestCase("file:///etc/passwd")]
    [TestCase("https://user:password@example.com/a.png")]
    public void InvalidAttachmentSchemeOrCredentialsRejected(string url) =>
        Assert.Throws<VisionInputException>(() => VisionInput.CreateMessage("", [url]));

    [TestCase("127.0.0.1", false)]
    [TestCase("10.0.0.1", false)]
    [TestCase("169.254.169.254", false)]
    [TestCase("100.66.88.34", false)]
    [TestCase("::1", false)]
    [TestCase("::ffff:192.168.1.1", false)]
    [TestCase("fd00::1", false)]
    [TestCase("8.8.8.8", true)]
    public void DownloadsDoNotConnectToPrivateAddresses(string address, bool expected) =>
        Assert.That(VisionInput.IsPublicAddress(IPAddress.Parse(address)), Is.EqualTo(expected));

    [TestCase(404)]
    [TestCase(302)]
    public void FailedOrRedirectedDownloadHasActionableError(int status)
    {
        using var http = new HttpClient(new ResponseHandler(() => new HttpResponseMessage((HttpStatusCode)status)));
        Assert.ThrowsAsync<VisionInputException>(() => new VisionInput(http).PrepareAsync(
            [VisionInput.CreateMessage("看图", ["https://example.com/a.png"])], true, 4, 1024, default));
    }

    [Test]
    public void OversizedAndNonImageContentRejected()
    {
        foreach (var bytes in new[] { new byte[1025], Encoding.UTF8.GetBytes("<html>not an image</html>") })
        {
            using var http = new HttpClient(new ResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new ByteArrayContent(bytes) }));
            Assert.ThrowsAsync<VisionInputException>(() => new VisionInput(http).PrepareAsync(
                [VisionInput.CreateMessage("看图", ["https://example.com/a.png"])], true, 4, 1024, default));
        }
    }

    [Test]
    public void OldHistoryLoadsWithoutAttachmentsAndNewHistoryRetainsThem()
    {
        var mapper = new BsonMapper();
        var old = mapper.ToObject<GroupMessage>(new BsonDocument
        { ["_id"] = "old", ["GroupId"] = "group", ["Content"] = "text" });
        Assert.That(old.ImageUrls, Is.Empty);
        old.ImageUrls = ["https://example.com/a.png"];
        var restored = mapper.ToObject<GroupMessage>(mapper.ToDocument(old));
        Assert.That(restored.ImageUrls, Is.EqualTo(old.ImageUrls));
    }

    [Test]
    public async Task OpenAICompatibleRequestContainsImageBlockInsteadOfPlainUrl()
    {
        var handler = new ChatHandler();
        using var http = new HttpClient(handler);
        var client = new OpenAIClient(new ApiKeyCredential("test"), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://example.com/v1"),
            Transport = new HttpClientPipelineTransport(http),
        }).GetChatClient("vision-test").AsIChatClient();
        var message = VisionInput.CreateMessage("左右是什么颜色？");
        message.Contents.Add(new DataContent(Png, "image/png"));
        await client.GetResponseAsync([message]);
        var content = JObject.Parse(handler.Body!)["messages"]![0]!["content"]!;
        Assert.That(content[0]!["text"]!.Value<string>(), Is.EqualTo("左右是什么颜色？"));
        Assert.That(content[1]!["type"]!.Value<string>(), Is.EqualTo("image_url"));
        Assert.That(content[1]!["image_url"]!["url"]!.Value<string>(), Does.StartWith("data:image/png;base64,"));
    }

    private sealed class ResponseHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response());
    }

    private sealed class ChatHandler : HttpMessageHandler
    {
        internal string? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"id":"test","object":"chat.completion","created":1,"model":"vision-test",
                     "choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}]}
                    """, Encoding.UTF8, "application/json"),
            };
        }
    }
}
