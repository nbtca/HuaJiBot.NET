using System.Net;
using System.Text;
using System.Text.Json;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Plugin.FlueBridge;
using Moq;

namespace HuaJiBot.NET.UnitTest;

public class FlueBridgeTest
{
    private string _path = null!;
    private BridgeStore _store = null!;
    private PluginConfig _config = null!;
    private FakeQueue _queue = null!;
    private Mock<IPluginService> _service = null!;
    private BridgeEngine _engine = null!;

    [SetUp]
    public void Setup()
    {
        _path = Path.Combine(Path.GetTempPath(), "flue-bridge-test-" + Guid.NewGuid(), "bridge.db");
        _store = new BridgeStore(_path);
        _config = new PluginConfig { Destinations = [new("bot", "-100:42")] };
        _queue = new FakeQueue();
        _service = new Mock<IPluginService>();
        _service.Setup(s => s.SendRichMessageAsync("bot", "-100:42", It.IsAny<RichContent>(), It.IsAny<Func<Task<SendingMessageBase[]>>>()))
            .ReturnsAsync(["sent-1", "sent-2"]);
        _engine = new BridgeEngine(_config, _store, _service.Object, _queue, _ => { });
    }

    [TearDown]
    public void Cleanup()
    {
        _store.Dispose();
        Directory.Delete(Path.GetDirectoryName(_path)!, true);
    }

    private static GroupMessageEventArgs Message(string id, params CommonCommandReader.ReaderEntity[] entities) =>
        new(() => new DefaultCommandReader(entities), () => ValueTask.FromResult("group"))
        {
            Service = new TestAdapter(),
            RobotId = "bot",
            MessageId = id,
            GroupId = "-100:42",
            SenderId = "user",
            SenderMemberCard = "Alice",
            TextMessageLazy = new(() => "question")
        };

    private ReplyJob Job(string id = "job-1")
    {
        var conversation = _engine.Normalize(Message("root", new CommonCommandReader.ReaderAt("bot"), "Question"))!.ConversationId;
        return new ReplyJob(1, id, conversation, new("main", "bot", "-100:42", "root"),
            new("markdown", "**answer**"), DateTimeOffset.UtcNow);
    }

    private static QueueLease Lease(ReplyJob job) => new(JsonSerializer.Serialize(job, BridgeJson.Options), "lease-1");

    [Test]
    public void SummaryDateAliasesWithOptionalSpacesUseSummaryRouting()
    {
        var index = 0;
        foreach (var day in new[] { "今", "昨", "前" })
            foreach (var suffix in new[] { "天", "日" })
                foreach (var gap in new[] { "", " ", "　" })
                    foreach (var inner in new[] { "", " ", "　" })
                    {
                        var text = "总结" + gap + day + inner + suffix;
                        var message = new GroupMessageEventArgs(() => new DefaultCommandReader([text]), () => ValueTask.FromResult("group"))
                        {
                            Service = new TestAdapter(),
                            RobotId = "bot",
                            GroupId = "-100:42",
                            MessageId = "alias-" + index++,
                            SenderId = "user",
                            SenderMemberCard = "Alice",
                            TextMessageLazy = new(() => text)
                        };
                        Assert.That(_engine.Normalize(message)!.Kind, Is.EqualTo("summary"), text);
                    }
    }

    [Test]
    public async Task OneBotGroupEventCarriesAccountIdAndReachesBridge()
    {
        var adapter = new HuaJiBot.NET.Adapter.OneBot.OneBotAdapter("ws://unused", null)
        { Logger = new HuaJiBot.NET.Logger.ConsoleLogger() };
        GroupMessageEventArgs? received = null;
        adapter.Events.OnGroupMessageReceived += (_, e) => received = e;
        var handler = new HuaJiBot.NET.Adapter.OneBot.OneBotMessageHandler(
            new HuaJiBot.NET.Adapter.OneBot.OneBotApi(adapter, _ => { }), adapter);
        await handler.ProcessMessageAsync(Newtonsoft.Json.Linq.JObject.Parse("""
            {"post_type":"message","message_type":"group","self_id":"bot",
             "group_id":"-100:42","user_id":"user","message_id":"real-event",
             "sender":{"nickname":"Alice","role":"member"},
             "message":[{"type":"at","data":{"qq":"bot"}},
                        {"type":"text","data":{"text":"question"}}]}
            """));
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.RobotId, Is.EqualTo("bot"));
        Assert.That(_engine.Normalize(received), Is.Not.Null);
    }

    [Test]
    public void MentionsCreateIsolatedThreadsAndRepliesResolveMappedMessages()
    {
        var first = _engine.Normalize(Message("root1", new CommonCommandReader.ReaderAt("bot"), "first"))!;
        var second = _engine.Normalize(Message("root2", new CommonCommandReader.ReaderAt("bot"), "second"))!;
        var reply = _engine.Normalize(Message("follow", new CommonCommandReader.ReaderReply(new("root1", senderId: "user", content: "first")), "follow-up"))!;
        Assert.Multiple(() =>
        {
            Assert.That(second.ConversationId, Is.Not.EqualTo(first.ConversationId));
            Assert.That(reply.ConversationId, Is.EqualTo(first.ConversationId));
            Assert.That(reply.ReplyTo!.Content, Is.EqualTo("first"));
            Assert.That(reply.Message.Text, Is.EqualTo("follow-up"));
            Assert.That(first.Destination.GroupId, Is.EqualTo("-100:42"));
            Assert.That(first.ConversationId, Does.Contain("-100%3A42"));
            Assert.That(_engine.Normalize(Message("other", "ordinary text"))!.Kind, Is.EqualTo("archive"));
            Assert.That(_engine.Normalize(Message("unknown", new CommonCommandReader.ReaderReply(new("missing")), "unknown"))!.Kind, Is.EqualTo("archive"));
        });
    }

    [Test]
    public void ArchiveOutboxSurvivesRestartAndKeepsOriginalPayload()
    {
        var message = _engine.Normalize(Message("archive", "ordinary text"))!;
        _store.SaveIngress(message);
        _store.SaveIngress(message with { Message = message.Message with { Text = "changed" } });
        _store.Dispose();
        _store = new BridgeStore(_path);
        var pending = _store.PendingIngress();
        Assert.That(pending, Has.Length.EqualTo(1));
        Assert.That(pending[0].Message.Text, Is.EqualTo(message.Message.Text));
        _store.RemoveIngress(message.EventId);
        Assert.That(_store.PendingIngress(), Is.Empty);
    }

    [Test]
    public async Task ScheduledSummaryUsesKnownAnchorWithoutPlatformReplyId()
    {
        _engine.SeedSummaryAnchors("bot", "-100:42");
        var anchor = "summary:" + DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd");
        var conversation = _store.Find("bot", "-100:42", anchor)!;
        var job = new ReplyJob(1, "scheduled", conversation, new("main", "bot", "-100:42", anchor), new("markdown", "summary"), DateTimeOffset.UtcNow, "summary");
        await _engine.DeliverAsync(Lease(job), CancellationToken.None);
        Assert.That(_store.WasSent("scheduled"), Is.True);
    }

    [Test]
    public async Task DeliveryPersistsAllIdsAndDuplicateLeaseIsAckedWithoutResending()
    {
        var job = Job();
        await _engine.DeliverAsync(Lease(job), CancellationToken.None);
        await _engine.DeliverAsync(Lease(job) with { LeaseId = "lease-2" }, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(_queue.Acks, Is.EqualTo(new[] { "lease-1", "lease-2" }));
            Assert.That(_store.Find("bot", "-100:42", "sent-1"), Is.EqualTo(job.ConversationId));
            Assert.That(_store.Find("bot", "-100:42", "sent-2"), Is.EqualTo(job.ConversationId));
            Assert.That(_store.WasSent(job.JobId), Is.True);
        });
        _service.Verify(s => s.SendRichMessageAsync("bot", "-100:42", new RichContent("**answer**", "root"),
            It.IsAny<Func<Task<SendingMessageBase[]>>>()), Times.Once);
    }

    [Test]
    public async Task LostAcknowledgmentAndRestartDoNotResend()
    {
        var job = Job();
        _queue.FailAck = true;
        Assert.ThrowsAsync<HttpRequestException>(() => _engine.DeliverAsync(Lease(job), CancellationToken.None));
        _store.Dispose();
        _store = new BridgeStore(_path);
        _engine = new BridgeEngine(_config, _store, _service.Object, _queue, _ => { });
        _queue.FailAck = false;
        await _engine.DeliverAsync(Lease(job) with { LeaseId = "new-lease" }, CancellationToken.None);
        Assert.That(_queue.Retries, Is.Empty);
        _service.Verify(s => s.SendRichMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RichContent>(),
            It.IsAny<Func<Task<SendingMessageBase[]>>>()), Times.Once);
    }

    [Test]
    public async Task AdapterFailureRetriesWithoutMarkingSentOrAcknowledging()
    {
        var job = Job();
        _service.Setup(s => s.SendRichMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RichContent>(),
            It.IsAny<Func<Task<SendingMessageBase[]>>>())).ThrowsAsync(new IOException("offline"));
        await _engine.DeliverAsync(Lease(job), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(_queue.Acks, Is.Empty);
            Assert.That(_queue.Retries, Is.EqualTo(new[] { "lease-1" }));
            Assert.That(_store.WasSent(job.JobId), Is.False);
        });
    }

    [Test]
    public async Task MalformedUnauthorizedAndOversizedJobsAreAckedAsPoison()
    {
        var job = Job();
        foreach (var poisoned in new[]
        {
            job with { Version = 2 }, job with { Destination = job.Destination with { BridgeInstance = "other" } },
            job with { Destination = job.Destination with { GroupId = "other" } },
            job with { ConversationId = "other" }, job with { Content = new("markdown", new string('x', 40000)) }
        }) await _engine.DeliverAsync(Lease(poisoned), CancellationToken.None);
        await _engine.DeliverAsync(new("not-json", "bad"), CancellationToken.None);
        Assert.That(_queue.Acks, Has.Count.EqualTo(6));
        _service.Verify(s => s.SendRichMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RichContent>(),
            It.IsAny<Func<Task<SendingMessageBase[]>>>()), Times.Never);
    }

    [Test]
    public void PruneRemovesOldMappingsAndCompletedRows()
    {
        var job = Job();
        _store.Complete(job, ["sent"]);
        _store.Prune(DateTimeOffset.UtcNow.AddDays(1));
        Assert.Multiple(() =>
        {
            Assert.That(_store.Find("bot", "-100:42", "root"), Is.Null);
            Assert.That(_store.WasSent(job.JobId), Is.False);
        });
    }

    [Test]
    public void PluginLoaderSkipsNativeRuntimeDlls()
    {
        var plugins = Path.Combine(Path.GetDirectoryName(_path)!, "plugins");
        var native = Path.Combine(plugins, "libs", "runtimes", "win-x64", "native");
        Directory.CreateDirectory(native);
        File.WriteAllText(Path.Combine(native, "e_sqlite3.dll"), "not a managed assembly");
        var manager = new HuaJiBot.NET.PluginManager.PluginManager();
        var load = manager.GetType().GetMethod("LoadAllPlugins", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Assert.DoesNotThrow(() => load.Invoke(manager, [_service.Object, new DirectoryInfo(plugins)]));
    }

    [Test]
    public void SignatureMatchesCrossLanguageGoldenVector()
    {
        Assert.That(IngressClient.Sign("test-secret", "1790942400", Encoding.UTF8.GetBytes("{\"text\":\"中文\"}")),
            Is.EqualTo("5e856ed96ab263401de4b3ae79b66202e00fb2c7d759596d8aa78d625a538030"));
    }

    [Test]
    public async Task HttpClientsUseExactSignedBodyAndCloudflareLeaseProtocol()
    {
        var requests = new List<(Uri Uri, string Body, string? Signature, string? Timestamp)>();
        using var http = new HttpClient(new CaptureHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            requests.Add((request.RequestUri!, body,
                request.Headers.TryGetValues("X-HuaJiBot-Signature", out var sig) ? sig.Single() : null,
                request.Headers.TryGetValues("X-HuaJiBot-Timestamp", out var ts) ? ts.Single() : null));
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true,\"result\":{\"messages\":[{\"body\":\"hello\",\"lease_id\":\"lease\"}]}}") };
        }));
        var message = _engine.Normalize(Message("签名", new CommonCommandReader.ReaderAt("bot"), "中文问题"))!;
        await new IngressClient(http, new Uri("https://agent.example/channels/huajibot/events"), "secret", 32768)
            .SendAsync(message, CancellationToken.None);
        var signed = requests[0];
        Assert.That(signed.Signature, Is.EqualTo("v1=" + IngressClient.Sign("secret", signed.Timestamp!, Encoding.UTF8.GetBytes(signed.Body))));
        var queue = new CloudflareQueue(http, new PluginConfig { AccountId = "account", QueueId = "queue" }, "token");
        await queue.PullAsync(CancellationToken.None);
        await queue.AckAsync("lease", CancellationToken.None);
        await queue.RetryAsync("lease", CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(requests[1].Uri.AbsolutePath, Is.EqualTo("/client/v4/accounts/account/queues/queue/messages/pull"));
            Assert.That(JsonDocument.Parse(requests[1].Body).RootElement.GetProperty("visibility_timeout").GetInt32(), Is.EqualTo(120000));
            Assert.That(requests[2].Body, Does.Contain("\"acks\":[{\"lease_id\":\"lease\"}]"));
            Assert.That(requests[3].Body, Does.Contain("\"delay_seconds\":10"));
        });
    }

    private sealed class FakeQueue : IReplyQueue
    {
        public List<string> Acks { get; } = [];
        public List<string> Retries { get; } = [];
        public bool FailAck { get; set; }
        public Task<IReadOnlyList<QueueLease>> PullAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<QueueLease>>([]);
        public Task AckAsync(string leaseId, CancellationToken ct)
        {
            if (FailAck) throw new HttpRequestException("Expired lease / lost ack");
            Acks.Add(leaseId);
            return Task.CompletedTask;
        }
        public Task RetryAsync(string leaseId, CancellationToken ct) { Retries.Add(leaseId); return Task.CompletedTask; }
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handle(request);
    }
}
