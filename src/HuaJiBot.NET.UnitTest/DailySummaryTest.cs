using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Plugin.DailySummary;
using HuaJiBot.NET.Plugin.DailySummary.Config;
using HuaJiBot.NET.Plugin.DailySummary.Service;

namespace HuaJiBot.NET.UnitTest;

internal class DailySummaryTest
{
    private static readonly DateTimeOffset Midnight = new(2026, 9, 23, 0, 5, 0, TimeSpan.FromHours(8));
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "huaji-dailysummary-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, true);

    private static GroupMessage Msg(int i) =>
        new()
        {
            MessageId = $"m{i}",
            GroupId = "g1",
            SenderId = $"u{i % 3}",
            SenderName = $"用户{i % 3}",
            Content = new string((char)('a' + i % 26), 20) + i,
            IsBot = false,
            ReplyToMessageId = null,
            Timestamp = new DateTime(2026, 9, 22, 10, 0, 0).AddMinutes(i),
        };

    [Test]
    public void Prompt_SmallDay_KeepsEverything()
    {
        var messages = Enumerable.Range(0, 5).Select(Msg).ToList();

        var chunks = SummaryPrompt.BuildChunks(messages, 20000, new DateTime(2026, 9, 22));

        Assert.That(chunks, Has.Count.EqualTo(1));
        Assert.That(chunks[0], Does.Contain("2026-09-22 的群聊记录"));
        Assert.That(chunks[0], Does.Contain(messages[0].Content));
        Assert.That(chunks[0], Does.Contain(messages[^1].Content));
    }

    [Test]
    public void Prompt_BigDay_SplitsInOrderWithoutDroppingMessages()
    {
        var messages = Enumerable.Range(0, 200).Select(Msg).ToList();
        var chunks = SummaryPrompt.BuildChunks(messages, 400, new DateTime(2026, 9, 22));

        Assert.That(chunks.Count, Is.GreaterThan(1));
        Assert.That(chunks.All(c => c.Length <= 400), Is.True);
        var combined = string.Join("\n", chunks);
        var offsets = messages
            .Select(m => combined.IndexOf($"[{SummaryPrompt.Time(m.Timestamp)}] {m.SenderName}: {m.Content}", StringComparison.Ordinal))
            .ToList();
        Assert.That(offsets.All(x => x >= 0), Is.True);
        Assert.That(offsets, Is.Ordered.Ascending);
    }

    [Test]
    public void Prompt_SingleHugeMessage_IsSplitWithoutLosingContent()
    {
        var message = Msg(0);
        message.Content = new string('x', 1000);
        var chunks = SummaryPrompt.BuildChunks([message], 256, new DateTime(2026, 9, 22));

        Assert.That(chunks.Count, Is.GreaterThan(1));
        Assert.That(chunks.All(c => c.Length <= 256), Is.True);
        Assert.That(chunks.Sum(c => c.Count(ch => ch == 'x')), Is.EqualTo(1000));
    }

    [Test]
    public async Task Pipeline_SummarizesEveryChunkThenCombinesTheResults()
    {
        var messages = Enumerable.Range(0, 30).Select(Msg).ToList();
        var requests = new List<string>();

        var result = await SummaryPipeline.RunAsync(
            messages,
            new DateTime(2026, 9, 22),
            400,
            "system",
            (_, prompt, _) =>
            {
                requests.Add(prompt);
                return Task.FromResult(prompt.Contains("分段摘要") ? "最终简报" : $"段{requests.Count}");
            },
            CancellationToken.None
        );

        Assert.That(result, Is.EqualTo("最终简报"));
        Assert.That(requests.Count, Is.GreaterThan(2));
        Assert.That(requests[^1], Does.Contain("段1"));
        Assert.That(requests[^1], Does.Contain($"段{requests.Count - 1}"));
        Assert.That(requests[0], Does.Contain(messages[0].Content));
        Assert.That(requests[^2], Does.Contain(messages[^1].Content));
    }

    [Test]
    public async Task Pipeline_SmallDay_UsesOnlyOneModelCall()
    {
        var calls = 0;
        var summary = await SummaryPipeline.RunAsync(
            [Msg(0)],
            new DateTime(2026, 9, 22),
            20000,
            "system",
            (_, _, _) =>
            {
                calls++;
                return Task.FromResult("简报");
            },
            CancellationToken.None
        );

        Assert.That(summary, Is.EqualTo("简报"));
        Assert.That(calls, Is.EqualTo(1));
    }

    private string StatePath => Path.Combine(_dir, "sent.json");

    private DailySummaryTask NewTask(
        Func<string, DateTime, CancellationToken, Task> summarize,
        params string[] groups
    ) =>
        new(
            new RecordingAdapter(),
            new PluginConfig { GroupIds = [.. groups], MaxRetryCount = 2, LlmTimeoutSeconds = 5 },
            StatePath,
            summarize
        );

    [Test]
    public async Task Schedule_SummarizesYesterdayOncePerDay()
    {
        var runs = new List<(string, DateTime)>();
        Task Record(string g, DateTime d, CancellationToken _)
        {
            runs.Add((g, d));
            return Task.CompletedTask;
        }

        await NewTask(Record, "g1", "g2").RunIfDueAsync(Midnight);
        await NewTask(Record, "g1", "g2").RunIfDueAsync(Midnight.AddMinutes(1));

        Assert.That(runs, Is.EqualTo(new[] { ("g1", new DateTime(2026, 9, 22)), ("g2", new DateTime(2026, 9, 22)) }));
    }

    [Test]
    public async Task Schedule_OutsideTheHour_DoesNothing()
    {
        var called = false;
        await NewTask((_, _, _) => Task.FromResult(called = true), "g1").RunIfDueAsync(Midnight.AddHours(1));

        Assert.That(called, Is.False);
    }

    [Test]
    public async Task Schedule_RetriesUpToTheLimit_WithoutBlockingOtherGroups()
    {
        var calls = new List<string>();
        var task = NewTask(
            (g, _, _) =>
            {
                calls.Add(g);
                return g == "bad" ? Task.FromException(new HttpRequestException("llm down")) : Task.CompletedTask;
            },
            "bad",
            "good"
        );

        for (var i = 0; i < 5; i++)
            await task.RunIfDueAsync(Midnight.AddMinutes(i));

        Assert.That(calls, Is.EqualTo(new[] { "bad", "good", "bad" }));
    }

    [Test]
    public async Task Schedule_SkipsWhileThePreviousRunIsStillGoing()
    {
        var gate = new TaskCompletionSource();
        var started = 0;
        var task = NewTask(
            async (_, _, _) =>
            {
                started++;
                await gate.Task;
            },
            "g1"
        );

        var first = task.RunIfDueAsync(Midnight);
        await task.RunIfDueAsync(Midnight.AddMinutes(1));
        gate.SetResult();
        await first;

        Assert.That(started, Is.EqualTo(1));
    }

    [Test]
    public async Task Schedule_NextDayStartsFresh()
    {
        var runs = new List<DateTime>();
        var task = NewTask(
            (_, d, _) =>
            {
                runs.Add(d);
                return Task.CompletedTask;
            },
            "g1"
        );

        await task.RunIfDueAsync(Midnight);
        await task.RunIfDueAsync(Midnight.AddDays(1));

        Assert.That(runs, Is.EqualTo(new[] { new DateTime(2026, 9, 22), new DateTime(2026, 9, 23) }));
    }

    [Test]
    public async Task Schedule_CorruptStateFile_IsIgnored()
    {
        File.WriteAllText(StatePath, "{ not json");
        var called = false;

        await NewTask((_, _, _) => Task.FromResult(called = true), "g1").RunIfDueAsync(Midnight);

        Assert.That(called, Is.True);
    }

    [Test]
    public void Commands_AreRegistered()
    {
        var commands = ((PluginBase)new PluginMain()).GetAllCommands();

        Assert.That(commands.Select(c => c.Name), Is.EqualTo(new[] { "总结" }));
    }
}
