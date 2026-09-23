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

        var result = SummaryPrompt.Build(messages, 20000, new DateTime(2026, 9, 22));

        Assert.That(result.KeptFrom, Is.Null);
        Assert.That(result.Text, Does.Contain("2026-09-22 的群聊记录"));
        Assert.That(result.Text, Does.Not.Contain("截断"));
        Assert.That(result.Text, Does.Contain(messages[0].Content));
    }

    [Test]
    public void Prompt_BigDay_KeepsNewestAndSaysSo()
    {
        var messages = Enumerable.Range(0, 200).Select(Msg).ToList();
        var budget = messages.Skip(150).Sum(m => m.Content!.Length + 30);

        var result = SummaryPrompt.Build(messages, budget, new DateTime(2026, 9, 22));

        Assert.That(result.KeptFrom, Is.Not.Null);
        Assert.That(result.Text, Does.Contain("记录已截断"));
        Assert.That(result.Text, Does.Contain(messages[199].Content));
        Assert.That(result.Text, Does.Not.Contain(messages[0].Content));
    }

    [Test]
    public void Prompt_SingleHugeMessage_IsStillSent()
    {
        var result = SummaryPrompt.Build([Msg(0)], 1, new DateTime(2026, 9, 22));

        Assert.That(result.KeptFrom, Is.Null);
        Assert.That(result.Text, Does.Contain(Msg(0).Content));
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
