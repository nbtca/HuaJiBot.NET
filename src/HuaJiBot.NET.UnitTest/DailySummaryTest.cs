using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Plugin.DailySummary;
using HuaJiBot.NET.Plugin.DailySummary.Config;
using HuaJiBot.NET.Plugin.DailySummary.Service;

namespace HuaJiBot.NET.UnitTest;

internal class DailySummaryTest
{
    private static readonly DateTimeOffset RunNow = new(
        2026,
        9,
        23,
        0,
        5,
        0,
        TimeSpan.FromHours(8)
    );
    private string _dir = null!;

    [SetUp]
    public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "huaji-dailysummary-" + Guid.NewGuid());

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    private static GroupMessage Msg(int i, int hour = 10) =>
        new()
        {
            MessageId = $"m{i}",
            GroupId = "g1",
            SenderId = $"u{i % 3}",
            SenderName = $"用户{i % 3}",
            Content = new string((char)('a' + i % 26), 20) + i,
            IsBot = false,
            ReplyToMessageId = null,
            Timestamp = new DateTime(2026, 9, 21, hour, 0, 0).AddMinutes(i),
        };

    // ---------- SummaryPrompt ----------

    [Test]
    public void Prompt_SmallDay_IsNotTruncated_AndCarriesDateHeader()
    {
        var messages = Enumerable.Range(0, 5).Select(i => Msg(i)).ToList();

        var result = SummaryPrompt.Build(messages, 20000, new DateTime(2026, 9, 21));

        Assert.That(result.Truncated, Is.False);
        Assert.That(result.Text, Does.Contain("2026-09-21 的群聊记录"));
        Assert.That(result.Text, Does.Not.Contain("截断"));
        Assert.That(result.Text, Does.Contain(messages[^1].Content));
    }

    [Test]
    public void Prompt_BigDay_KeepsNewestAndTellsLlmAboutTruncation()
    {
        var messages = Enumerable.Range(0, 200).Select(i => Msg(i)).ToList();
        var budget = messages
            .Skip(150)
            .Sum(m => m.Content!.Length + 30);

        var result = SummaryPrompt.Build(messages, budget, new DateTime(2026, 9, 21));

        Assert.That(result.Truncated, Is.True);
        Assert.That(result.Text, Does.Contain("记录已截断"));
        Assert.That(result.Text, Does.Contain(messages[199].Content));
        Assert.That(result.Text, Does.Not.Contain(messages[0].Content));
    }

    [Test]
    public void Prompt_EmptyList_DoesNotCrash()
    {
        var result = SummaryPrompt.Build([], 100, new DateTime(2026, 9, 21));

        Assert.That(result.Truncated, Is.False);
        Assert.That(result.Text, Does.Contain("2026-09-21 的群聊记录"));
    }

    // ---------- SummaryState ----------

    [Test]
    public void State_FailureCountsUp_SentStopsRetry()
    {
        var path = Path.Combine(_dir, "state.json");
        var state = new SummaryState(path);
        state.RollOver(DateOnly.FromDateTime(RunNow.Date));

        Assert.That(state.IsDue("g1", 2), Is.True);
        state.MarkFailure("g1");
        Assert.That(state.IsDue("g1", 2), Is.True);
        state.MarkFailure("g1");
        Assert.That(state.IsDue("g1", 2), Is.False, "达到重试上限后当天放弃");

        state.MarkSent("g2");
        Assert.That(state.IsDue("g2", 5), Is.False);
    }

    [Test]
    public void State_SurvivesRestart()
    {
        var path = Path.Combine(_dir, "state.json");
        var first = new SummaryState(path);
        first.RollOver(DateOnly.FromDateTime(RunNow.Date));
        first.MarkSent("g1");
        first.MarkFailure("g2");
        first.Save();

        var reopened = new SummaryState(path);
        reopened.RollOver(DateOnly.FromDateTime(RunNow.Date));

        Assert.That(reopened.IsDue("g1", 3), Is.False);
        Assert.That(reopened.Get("g2").Attempts, Is.EqualTo(1));
    }

    [Test]
    public void State_NewDay_ClearsAllRecords()
    {
        var path = Path.Combine(_dir, "state.json");
        var state = new SummaryState(path);
        state.RollOver(DateOnly.FromDateTime(RunNow.Date));
        state.MarkSent("g1");
        state.Save();

        state.RollOver(DateOnly.FromDateTime(RunNow.Date.AddDays(1)));

        Assert.That(state.IsDue("g1", 3), Is.True, "翻日即清空，前一天成功不影响今天");

        var reloaded = new SummaryState(path);
        reloaded.RollOver(DateOnly.FromDateTime(RunNow.Date.AddDays(1)));
        Assert.That(reloaded.IsDue("g1", 3), Is.True, "清空已持久化");
    }

    [Test]
    public void State_CorruptFile_FallsBackToEmpty()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "state.json");
        File.WriteAllText(path, "{ not json");

        var state = new SummaryState(path);

        Assert.That(state.IsDue("g1", 1), Is.True);
    }

    // ---------- DailySummaryTask ----------

    private sealed record Run(string Group, DateTime TargetDate);

    private static DailySummaryTask NewTask(
        PluginConfig config,
        SummaryState state,
        Func<string, DateTime, CancellationToken, Task<bool>> generate
    ) =>
        new(new RecordingAdapter(), config, state, generate);

    private static PluginConfig TestConfig(params string[] groups) =>
        new()
        {
            GroupIds = groups.ToList(),
            SummaryHour = 0,
            SummaryMinute = 0,
            SummaryDaysAgo = 2,
            MaxRetryCount = 2,
            LlmTimeoutSeconds = 5,
            MaxConcurrentGroups = 2,
        };

    [Test]
    public async Task Schedule_RunsDueDaySummariesAndMarksSent()
    {
        var state = new SummaryState(Path.Combine(_dir, "state.json"));
        var runs = new List<Run>();
        var task = NewTask(
            TestConfig("g1", "g2"),
            state,
            (g, d, _) =>
            {
                lock (runs)
                    runs.Add(new Run(g, d));
                return Task.FromResult(true);
            }
        );

        await task.RunIfDueAsync(RunNow);

        Assert.That(runs.Select(r => r.Group), Is.EquivalentTo(new[] { "g1", "g2" }));
        Assert.That(runs.All(r => r.TargetDate == new DateTime(2026, 9, 21)), Is.True, "零点总结前天");
        Assert.That(state.IsDue("g1", 5), Is.False);

        runs.Clear();
        await task.RunIfDueAsync(RunNow.AddMinutes(1));
        Assert.That(runs, Is.Empty, "已成功不再重发");
    }

    [Test]
    public async Task Schedule_OutsideWindow_DoesNothing()
    {
        var state = new SummaryState(Path.Combine(_dir, "state.json"));
        var called = 0;
        var task = NewTask(
            TestConfig("g1"),
            state,
            (_, _, _) =>
            {
                called++;
                return Task.FromResult(true);
            }
        );

        await task.RunIfDueAsync(new DateTimeOffset(2026, 9, 23, 1, 5, 0, TimeSpan.FromHours(8)));

        Assert.That(called, Is.Zero);
    }

    [Test]
    public async Task Schedule_RetriesUntilMaxAttemptsThenGivesUpForToday()
    {
        var state = new SummaryState(Path.Combine(_dir, "state.json"));
        var task = NewTask(
            TestConfig("g1"),
            state,
            (_, _, _) => Task.FromException<bool>(new HttpRequestException("llm down"))
        );

        for (var i = 0; i < 5; i++)
            await task.RunIfDueAsync(RunNow.AddMinutes(i));

        Assert.That(state.Get("g1").Attempts, Is.EqualTo(2), "只重试到上限");
        Assert.That(state.Get("g1").Sent, Is.False);
    }

    [Test]
    public async Task Schedule_SkipsGroupAlreadyInFlight()
    {
        var state = new SummaryState(Path.Combine(_dir, "state.json"));
        var gate = new TaskCompletionSource();
        var started = 0;
        var task = NewTask(
            TestConfig("g1"),
            state,
            async (_, _, _) =>
            {
                Interlocked.Increment(ref started);
                await gate.Task;
                return true;
            }
        );

        var first = task.RunIfDueAsync(RunNow);
        await task.RunIfDueAsync(RunNow.AddMinutes(1));
        gate.SetResult();
        await first;

        Assert.That(Volatile.Read(ref started), Is.EqualTo(1), "上一轮未跑完不叠加执行");
    }

    [Test]
    public async Task Schedule_GroupFailureDoesNotBlockOthers()
    {
        var state = new SummaryState(Path.Combine(_dir, "state.json"));
        var okGroups = new List<string>();
        var task = NewTask(
            TestConfig("bad", "good"),
            state,
            (g, _, _) =>
                g == "bad"
                    ? Task.FromException<bool>(new Exception("boom"))
                    : Task.Run(() =>
                    {
                        lock (okGroups)
                            okGroups.Add(g);
                        return true;
                    })
        );

        await task.RunIfDueAsync(RunNow);

        Assert.That(okGroups, Is.EqualTo(new[] { "good" }));
        Assert.That(state.Get("bad").Attempts, Is.EqualTo(1));
        Assert.That(state.Get("good").Sent, Is.True);
    }

    [Test]
    public async Task Schedule_NewDayResumesWithFreshState()
    {
        var state = new SummaryState(Path.Combine(_dir, "state.json"));
        var runs = new List<DateTime>();
        var task = NewTask(
            TestConfig("g1"),
            state,
            (_, d, _) =>
            {
                runs.Add(d);
                return Task.FromResult(true);
            }
        );
        await task.RunIfDueAsync(RunNow);
        state.Save();

        var nextDay = new DateTimeOffset(2026, 9, 24, 0, 5, 0, TimeSpan.FromHours(8));
        await task.RunIfDueAsync(nextDay);

        Assert.That(runs, Is.EqualTo(new[] { new DateTime(2026, 9, 21), new DateTime(2026, 9, 22) }));
    }

    [Test]
    public void Config_Defaults_MatchPilotAgreement()
    {
        var config = new PluginConfig();

        Assert.That(config.SummaryDaysAgo, Is.EqualTo(2), "默认零点总结前天");
        Assert.That(config.MaxRetryCount, Is.EqualTo(3));
        Assert.That(config.LlmTimeoutSeconds, Is.EqualTo(180));
        Assert.That(config.SystemPrompt, Does.Contain("截断"));
    }

    [Test]
    public void Commands_AreRegisteredBySourceGenerator()
    {
        var plugin = new PluginMain();

        var commands = ((PluginBase)plugin).GetAllCommands().ToArray();

        Assert.That(
            commands.Select(c => c.Name),
            Is.EqualTo(new[] { "总结" }),
            "GetAllCommands 必须由源生成器重写并注册手动总结命令"
        );
    }
}
