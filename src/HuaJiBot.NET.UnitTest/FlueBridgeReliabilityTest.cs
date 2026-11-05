using System.Net;
using System.Text.Json;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Plugin.FlueBridge;
using Microsoft.Data.Sqlite;

namespace HuaJiBot.NET.UnitTest;

public partial class FlueBridgeTest
{
    [TestCase(400)]
    [TestCase(403)]
    [TestCase(409)]
    [TestCase(413)]
    public async Task PermanentIngressFailuresKeepAuditButDoNotBlockTheNextPage(int status)
    {
        for (var i = 0; i < 51; i++)
            _store.SaveIngress(_engine.Normalize(Message("outbox-" + i, "archive"))!);
        using var http = new HttpClient(new CaptureHandler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status))));
        var forwarder = new IngressForwarder(_store,
            new IngressClient(http, new Uri("https://agent.example/events"), "test", 32768), 10);
        foreach (var message in _store.PendingIngress())
            Assert.That(await forwarder.ForwardAsync(message, CancellationToken.None), Is.False);
        _store.Dispose();
        _store = new BridgeStore(_path);
        Assert.That(_store.PendingIngress(DateTimeOffset.UtcNow.AddYears(1)).Select(x => x.Message.MessageId),
            Is.EqualTo(new[] { "outbox-50" }));
        using var db = new SqliteConnection($"Data Source={_path};Pooling=False");
        db.Open();
        using var audit = db.CreateCommand();
        audit.CommandText = "SELECT COUNT(*) FROM ingress_outbox JOIN ingress_failures USING(event_id) WHERE rejected_at IS NOT NULL AND status_code=$s";
        audit.Parameters.AddWithValue("$s", status);
        Assert.That((long)audit.ExecuteScalar()!, Is.EqualTo(50));
    }

    [TestCase(401)]
    [TestCase(408)]
    [TestCase(429)]
    [TestCase(503)]
    public async Task TransientIngressFailuresBackOffAndRecover(int status)
    {
        var message = _engine.Normalize(Message("retry-ingress", "archive"))!;
        _store.SaveIngress(message);
        var responseStatus = (HttpStatusCode)status;
        using var http = new HttpClient(new CaptureHandler(_ => Task.FromResult(new HttpResponseMessage(responseStatus))));
        var forwarder = new IngressForwarder(_store,
            new IngressClient(http, new Uri("https://agent.example/events"), "test", 32768), 10);
        Assert.That(await forwarder.ForwardAsync(message, CancellationToken.None), Is.False);
        Assert.That(_store.PendingIngress(), Is.Empty);
        Assert.That(_store.PendingIngress(DateTimeOffset.UtcNow.AddHours(2)), Has.Length.EqualTo(1));
        responseStatus = HttpStatusCode.OK;
        Assert.That(await forwarder.ForwardAsync(message, CancellationToken.None), Is.True);
        Assert.That(_store.PendingIngress(DateTimeOffset.UtcNow.AddYears(1)), Is.Empty);
    }

    private CommandJob Command(string id = "help", string arguments = "{}", MemberType role = MemberType.Member)
    {
        var source = new GroupMessageEventArgs(() => new DefaultCommandReader([new CommonCommandReader.ReaderAt("bot"), "command"]),
            () => ValueTask.FromResult("group"))
        {
            Service = new TestAdapter(),
            RobotId = "bot",
            GroupId = "-100:42",
            MessageId = "command-source",
            SenderId = "user",
            SenderMemberCard = "trusted name",
            SenderMemberType = role,
            TextMessageLazy = new(() => "command"),
        };
        var ingress = _engine.Normalize(source)!;
        return new CommandJob(2, "command-job", ingress.ConversationId, new("main", "bot", "-100:42", source.MessageId),
            source.SenderId, "untrusted queue name", new(id, JsonDocument.Parse(arguments).RootElement.Clone()), DateTimeOffset.UtcNow);
    }

    private static QueueLease CommandLease(CommandJob job) => new(JsonSerializer.Serialize(job, BridgeJson.Options), "command-lease");

    private BridgeEngine CommandEngine(BotService adapter) => new(_config, _store, adapter, _queue, _ => { },
        new CommandExecutor(_store, adapter).ExecuteAsync);

    private static T CreatePlugin<T>(BotService adapter) where T : PluginBase, new() =>
        (T)new PluginEntryPointAttribute<T>(typeof(T).Namespace!, "test").CreateInstance(adapter);

    [Test]
    public async Task HelpWaitsForReceiptBeforeAckAndItsReplyContinuesTheConversation()
    {
        var pending = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapter = new CommandAdapter { Send = _ => pending.Task };
        var job = Command();
        var engine = CommandEngine(adapter);
        var delivery = engine.DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(delivery.IsCompleted, Is.False);
            Assert.That(_queue.Acks, Is.Empty);
            Assert.That(_store.WasSent(job.JobId), Is.False);
        });
        pending.SetResult(["help-part-1", "help-part-2"]);
        await delivery;
        var followup = engine.Normalize(Message("follow-help", new CommonCommandReader.ReaderReply(new("help-part-2")), "follow-up"))!;
        Assert.Multiple(() =>
        {
            Assert.That(_queue.Acks, Has.Count.EqualTo(1));
            Assert.That(_store.Find("bot", "-100:42", "help-part-1"), Is.EqualTo(job.ConversationId));
            Assert.That(followup.Kind, Is.EqualTo("chat"));
            Assert.That(followup.ConversationId, Is.EqualTo(job.ConversationId));
        });
        _queue.FailAck = true;
        Assert.ThrowsAsync<HttpRequestException>(() => engine.DeliverAsync(CommandLease(job), CancellationToken.None));
        _store.Dispose();
        _store = new BridgeStore(_path);
        _queue.FailAck = false;
        await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.That(adapter.Sends, Has.Count.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FailedOrEmptyCommandSendsAreRetriedWithoutCompleting(bool emptyReceipt)
    {
        var adapter = new CommandAdapter
        {
            Send = _ => emptyReceipt ? Task.FromResult<string[]>([]) : Task.FromException<string[]>(new IOException("offline")),
        };
        var job = Command();
        await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(_queue.Acks, Is.Empty);
            Assert.That(_queue.Retries, Is.EqualTo(new[] { "command-lease" }));
            Assert.That(_store.WasSent(job.JobId), Is.False);
        });
        adapter.Send = _ => Task.FromResult<string[]>(["recovered"]);
        await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.That(_store.Find("bot", "-100:42", "recovered"), Is.EqualTo(job.ConversationId));
    }

    [Test]
    public async Task SuccessfulCommandRepliesAreNotResentAfterAPartialFailureAndRestart()
    {
        var job = Command();
        var sends = 0;
        Task<string[]> Send() { sends++; return Task.FromResult<string[]>(["first-reply"]); }
        await CommandExecutor.SendReplyAsync(_store, job, "first", Send);
        Assert.ThrowsAsync<IOException>(() => CommandExecutor.SendReplyAsync(_store, job, "second",
            () => Task.FromException<string[]>(new IOException("offline"))));
        _store.Dispose();
        _store = new BridgeStore(_path);
        await CommandExecutor.SendReplyAsync(_store, job, "first", Send);
        Assert.That(sends, Is.EqualTo(1));
        Assert.That(_store.Find("bot", "-100:42", "first-reply"), Is.EqualTo(job.ConversationId));
    }

    [TestCase(MemberType.Admin, true)]
    [TestCase(MemberType.Owner, true)]
    [TestCase(MemberType.Member, false)]
    public async Task EventsUsesPersistedLocalRoleAndAllowsOmittedArguments(MemberType role, bool authorized)
    {
        var adapter = new CommandAdapter();
        adapter.SetupCommands(CreatePlugin<HuaJiBot.NET.Plugin.MessageBridge.PluginMain>(adapter));
        var job = Command("events", role: role);
        _store.Dispose();
        _store = new BridgeStore(_path);
        await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
        var text = ((TextMessage)adapter.Sends.Single().Last()).Text;
        Assert.That(text.Contains("只有群管理员"), Is.EqualTo(!authorized));
        if (authorized) Assert.That(text, Does.Contain("类型 可选"));
        Assert.That(_store.WasSent(job.JobId), Is.True);
    }

    [Test]
    public async Task QueueCannotChangeTheLocallyRecordedCommandSender()
    {
        var adapter = new CommandAdapter();
        var job = Command() with { SenderId = "other-admin" };
        await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.That(adapter.Sends, Is.Empty);
        Assert.That(_queue.Acks, Has.Count.EqualTo(1));
        Assert.That(_store.WasSent(job.JobId), Is.False);
    }

    [Test]
    public async Task InvalidCommandArgumentReplyAlsoRequiresReceiptAndMapsConversation()
    {
        var adapter = new CommandAdapter { Send = _ => Task.FromException<string[]>(new IOException("offline")) };
        var job = Command();
        var engine = new BridgeEngine(_config, _store, adapter, _queue, _ => { },
            (_, _) => throw new ArgumentException("bad arguments"));
        await engine.DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.That(_queue.Acks, Is.Empty);
        Assert.That(_queue.Retries, Has.Count.EqualTo(1));
        adapter.Send = _ => Task.FromResult<string[]>(["argument-error"]);
        await engine.DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.That(_store.Find("bot", "-100:42", "argument-error"), Is.EqualTo(job.ConversationId));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ScheduleWithoutWeeksUsesTheRealPluginsOneWeekDefault(bool failFirstSend)
    {
        var adapter = new CommandAdapter();
        var plugin = CreatePlugin<HuaJiBot.NET.Plugin.Calendar.PluginMain>(adapter);
        // Seed the existing calendar cache so this is an entirely local command test.
        var sync = ((Lazy<HuaJiBot.NET.Plugin.Calendar.RemoteSync>)typeof(HuaJiBot.NET.Plugin.Calendar.PluginMain)
            .GetField("_sync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(plugin)!).Value;
        typeof(HuaJiBot.NET.Plugin.Calendar.RemoteSync).GetField("_lastLoadTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(sync, HuaJiBot.NET.Utils.NetworkTime.Now);
        typeof(HuaJiBot.NET.Plugin.Calendar.RemoteSync).GetProperty("Calendar")!.SetValue(sync, new Ical.Net.Calendar());
        adapter.SetupCommands(plugin);
        var job = Command("schedule");
        if (failFirstSend)
        {
            adapter.Send = _ => Task.FromException<string[]>(new IOException("offline"));
            await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
            Assert.That(_store.WasSent(job.JobId), Is.False);
            adapter.Send = _ => Task.FromResult<string[]>(["schedule-retry"]);
        }
        await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.That(((TextMessage)adapter.Sends.Last().Last()).Text, Does.StartWith("近1周的日程"));
        Assert.That(_store.WasSent(job.JobId), Is.True);
    }

    [Test]
    public async Task EventsCanQueryOneTypeWithoutEnabled()
    {
        var adapter = new CommandAdapter();
        adapter.SetupCommands(CreatePlugin<HuaJiBot.NET.Plugin.MessageBridge.PluginMain>(adapter));
        var job = Command("events", """{"type":"死亡"}""", MemberType.Admin);
        await CommandEngine(adapter).DeliverAsync(CommandLease(job), CancellationToken.None);
        Assert.That(((TextMessage)adapter.Sends.Single().Last()).Text, Does.StartWith("状态 可选"));
    }

    [Test]
    public async Task MinecraftTimeoutReplyIsAwaitedAndPropagatesSendFailure()
    {
        var pending = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapter = new CommandAdapter { Send = _ => pending.Task };
        var e = new GroupMessageEventArgs(() => new DefaultCommandReader([]), () => ValueTask.FromResult("group"))
        {
            Service = adapter,
            RobotId = "bot",
            GroupId = "group",
            MessageId = "source",
            SenderId = "user",
            SenderMemberCard = "user",
            TextMessageLazy = new(() => ""),
        };
        var reply = HuaJiBot.NET.Plugin.MessageBridge.PluginMain.ReplyPlayerListAsync(e, []);
        Assert.That(reply.IsCompleted, Is.False);
        pending.SetException(new IOException("offline"));
        try { await reply; Assert.Fail("Expected the adapter failure"); }
        catch (IOException) { }
    }

    [Test]
    public void UpgradeAddsTablesWithoutLosingExistingOutboxOrDeliveryData()
    {
        var reply = Job();
        _store.Complete(reply, ["existing-reply"]);
        var ingress = _engine.Normalize(Message("existing-outbox", "archive"))!;
        _store.SaveIngress(ingress);
        _store.Dispose();
        using (var db = new SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            db.Open();
            using var drop = db.CreateCommand();
            drop.CommandText = "DROP TABLE ingress_failures; DROP TABLE command_origins; DROP TABLE command_reply_receipts";
            drop.ExecuteNonQuery();
        }
        _store = new BridgeStore(_path);
        Assert.That(_store.PendingIngress().Single().EventId, Is.EqualTo(ingress.EventId));
        Assert.That(_store.WasSent(reply.JobId), Is.True);
        Assert.That(_store.Find("bot", "-100:42", "existing-reply"), Is.EqualTo(reply.ConversationId));
    }

    private sealed class CommandAdapter : TestAdapter
    {
        internal Func<SendingMessageBase[], Task<string[]>> Send { get; set; } = _ => Task.FromResult<string[]>(["command-result"]);
        internal List<SendingMessageBase[]> Sends { get; } = [];
        public override Task<string[]> SendGroupMessageAsync(string? robotId, string targetGroup, params SendingMessageBase[] messages)
        {
            Sends.Add(messages);
            return Send(messages);
        }
        public override Task<string[]> FeedbackAt(string? robotId, string targetGroup, string msgId, string text) =>
            SendGroupMessageAsync(robotId, targetGroup, new ReplyMessage(msgId), new TextMessage(text));
    }
}
