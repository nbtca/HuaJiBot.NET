using System.Net;
using HuaJiBot.NET.Plugin.RepairTeam;

namespace HuaJiBot.NET.UnitTest;

internal class TicketReminderTest
{
    private static readonly DateTimeOffset Since = new(2026, 9, 1, 0, 0, 0, TimeSpan.FromHours(8));

    private sealed class StubHandler(Func<string, (HttpStatusCode, string)> respond)
        : HttpMessageHandler
    {
        public readonly List<string> Requests = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var path = request.RequestUri!.PathAndQuery;
            Requests.Add(path);
            var (code, body) = respond(path);
            return Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body) });
        }
    }

    private static string Summary(long id, string created) =>
        $$"""{"eventId":{{id}},"status":"open","model":"m{{id}}","problem":"p{{id}}","member":null,"logs":null,"gmtCreate":"{{created}}"}""";

    private static string Detail(long id, string status, string created, params string[] logTimes) =>
        $$"""
        {"eventId":{{id}},"status":"{{status}}","model":"m{{id}}","problem":"p{{id}}",
         "member":{"alias":"队员{{id}}"},"gmtCreate":"{{created}}",
         "logs":[{{string.Join(",", logTimes.Select(t => $$"""{"action":"x","gmtCreate":"{{t}}"}"""))}}]}
        """;

    private static readonly DateTimeOffset Noon = new(2026, 9, 22, 12, 0, 0, TimeSpan.FromHours(8));

    private static Ticket T(long id, string status, double daysIdle, string? member = null) =>
        new(id, status, $"m{id}", $"p{id}", member, Noon.AddDays(-10), Noon.AddDays(-daysIdle));

    [TestCase("open", 1.01, true)]
    [TestCase("open", 1, false)]
    [TestCase("accepted", 2.9, false)]
    [TestCase("accepted", 3.1, true)]
    [TestCase("committed", 2.1, true)]
    [TestCase("committed", 1.9, false)]
    public void Stalled_UsesPerStatusThresholdOnLastActivity(string status, double daysIdle, bool stalled)
    {
        var result = TicketDigest.Stalled([T(1, status, daysIdle)], Noon, new PluginConfig());

        Assert.That(result, Has.Count.EqualTo(stalled ? 1 : 0));
    }

    [Test]
    public void Format_GroupsByStageAndLinksThePortal()
    {
        var text = TicketDigest.Format(
            "【维修工单提醒】3 张工单卡住了",
            [
                new(785, "committed", "小新14", "清灰", null, Noon, Noon.AddDays(-3.5)),
                new(801, "open", "拯救者", "换硅脂", null, Noon, Noon.AddDays(-2.2)),
                new(790, "accepted", "天选4", "清灰", "Skillful Li", Noon, Noon.AddDays(-5)),
            ],
            Noon
        );

        Assert.That(
            text,
            Is.EqualTo(
                """
                【维修工单提醒】3 张工单卡住了
                待接单：
                  #801 拯救者 · 换硅脂 · 已 2 天
                已接单未提交：
                  #790 天选4 · 清灰 · Skillful Li · 已 5 天
                待审核：
                  #785 小新14 · 清灰 · 已 3 天
                去处理：https://repair.nbtca.space
                """.ReplaceLineEndings("\n")
            )
        );
    }

    [Test]
    public void Format_ShowsUnderOneDayAndSkipsMissingFields()
    {
        var text = TicketDigest.Format("t", [new(9, "open", null, "蓝屏", null, Noon, Noon.AddHours(-3))], Noon);

        Assert.That(text, Does.Contain("  #9 蓝屏 · 不到 1 天\n"));
        Assert.That(text, Does.Not.Contain("已接单未提交").And.Not.Contain("待审核"));
    }

    private static PluginConfig ReminderConfig() =>
        new()
        {
            PushInfoGroup = ["events-only"],
            RemindGroups = ["repair", "debug"],
            RemindSince = Since,
        };

    private static string SentText(RecordingAdapter adapter, string group) =>
        string.Concat(
            adapter.Sends.Where(x => x.Target == group).SelectMany(x => x.Messages).OfType<Bot.TextMessage>().Select(x => x.Text)
        );

    private static string TempState() => Path.Combine(Path.GetTempPath(), $"ticket_reminder_{Guid.NewGuid():N}.json");

    [Test]
    public async Task Reminder_SendsOncePerDay_EvenAfterRestart()
    {
        var adapter = new RecordingAdapter();
        var state = TempState();
        var config = ReminderConfig();
        Task<List<Ticket>> Fetch(DateTimeOffset _) => Task.FromResult(new List<Ticket> { T(1, "open", 2) });

        await new TicketReminder(adapter, config, Fetch, state).CheckAsync(Noon);
        await new TicketReminder(adapter, config, Fetch, state).CheckAsync(Noon.AddMinutes(30));

        Assert.That(adapter.Sends.Select(x => x.Target), Is.EqualTo(new[] { "repair", "debug" }));
        Assert.That(SentText(adapter, "repair"), Does.StartWith("【维修工单提醒】1 张工单卡住了").And.Contain("#1 "));
    }

    [Test]
    public async Task Reminder_OutsideHourOrWithoutSince_DoesNothing()
    {
        var adapter = new RecordingAdapter();
        var fetched = 0;
        Task<List<Ticket>> Fetch(DateTimeOffset _)
        {
            fetched++;
            return Task.FromResult(new List<Ticket> { T(1, "open", 2) });
        }

        await new TicketReminder(adapter, ReminderConfig(), Fetch, TempState()).CheckAsync(Noon.AddHours(-1));
        await new TicketReminder(adapter, new PluginConfig { RemindGroups = ["repair"] }, Fetch, TempState()).CheckAsync(Noon);

        Assert.That(adapter.Sends, Is.Empty);
        Assert.That(fetched, Is.Zero);
    }

    [Test]
    public async Task Reminder_NothingStalled_SendsNothingButCountsTheDay()
    {
        var adapter = new RecordingAdapter();
        var reminder = new TicketReminder(adapter, ReminderConfig(), _ => Task.FromResult(new List<Ticket> { T(1, "open", 0.2) }), TempState());

        Assert.That(await reminder.SendDigestAsync(Noon), Is.True);
        Assert.That(adapter.Sends, Is.Empty);
    }

    [Test]
    public async Task Reminder_FetchFailure_IsRetriedNextCheck()
    {
        var adapter = new RecordingAdapter();
        var calls = 0;
        Task<List<Ticket>> Fetch(DateTimeOffset _) =>
            ++calls == 1
                ? throw new HttpRequestException("down")
                : Task.FromResult(new List<Ticket> { T(1, "committed", 5) });
        var reminder = new TicketReminder(adapter, ReminderConfig(), Fetch, TempState());

        await reminder.CheckAsync(Noon);
        Assert.That(adapter.Sends, Is.Empty);

        await reminder.CheckAsync(Noon.AddMinutes(10));
        Assert.That(SentText(adapter, "repair"), Does.Contain("待审核"));
    }

    [Test]
    public async Task Client_StopsAtFirstTicketOlderThanSince_AndUsesLatestLog()
    {
        var handler = new StubHandler(path =>
            path switch
            {
                _ when path.StartsWith("/events?status=open") => (
                    HttpStatusCode.OK,
                    $"[{Summary(12, "2026-09-20T10:00:00+08:00")},{Summary(11, "2026-08-30T10:00:00+08:00")},{Summary(10, "2026-08-01T10:00:00+08:00")}]"
                ),
                _ when path.StartsWith("/events?") => (HttpStatusCode.OK, "[]"),
                "/events/12" => (
                    HttpStatusCode.OK,
                    Detail(12, "accepted", "2026-09-20T10:00:00+08:00", "2026-09-20T10:00:00+08:00", "2026-09-21T09:00:00+08:00")
                ),
                _ => (HttpStatusCode.NotFound, ""),
            }
        );
        var client = new SaturdayClient(new HttpClient(handler), "https://api.test", (_, _) => { });

        var tickets = await client.GetActiveTicketsAsync(Since);

        Assert.That(handler.Requests, Does.Contain("/events?status=open&order=DESC&limit=50&offset=0"));
        Assert.That(handler.Requests, Does.Not.Contain("/events/11"));
        var t = tickets.Single();
        Assert.Multiple(() =>
        {
            Assert.That(t.Id, Is.EqualTo(12));
            Assert.That(t.Status, Is.EqualTo("accepted"));
            Assert.That(t.Member, Is.EqualTo("队员12"));
            Assert.That(t.LastActivity, Is.EqualTo(new DateTimeOffset(2026, 9, 21, 9, 0, 0, TimeSpan.FromHours(8))));
        });
    }

    [Test]
    public async Task Client_WithoutLogs_FallsBackToCreateTime_AndSkipsFailedDetails()
    {
        var errors = new List<string>();
        var handler = new StubHandler(path =>
            path switch
            {
                _ when path.StartsWith("/events?status=committed") => (
                    HttpStatusCode.OK,
                    $"[{Summary(21, "2026-09-10T10:00:00+08:00")},{Summary(20, "2026-09-09T10:00:00+08:00")}]"
                ),
                _ when path.StartsWith("/events?") => (HttpStatusCode.OK, "[]"),
                "/events/21" => (HttpStatusCode.InternalServerError, ""),
                "/events/20" => (HttpStatusCode.OK, Detail(20, "committed", "2026-09-09T10:00:00+08:00")),
                _ => (HttpStatusCode.NotFound, ""),
            }
        );
        var client = new SaturdayClient(new HttpClient(handler), "https://api.test", (msg, _) => errors.Add(msg));

        var tickets = await client.GetActiveTicketsAsync(Since);

        Assert.That(tickets.Select(x => x.Id), Is.EqualTo(new long[] { 20 }));
        Assert.That(tickets[0].LastActivity, Is.EqualTo(tickets[0].Created));
        Assert.That(errors, Has.Count.EqualTo(1).And.Some.Contains("#21"));
    }

    [Test]
    public async Task Client_PagesUntilAPageEndsBeforeSince()
    {
        var handler = new StubHandler(path =>
        {
            if (path.StartsWith("/events?status=open&order=DESC&limit=50&offset=0"))
                return (
                    HttpStatusCode.OK,
                    "[" + string.Join(",", Enumerable.Range(0, 50).Select(i => Summary(200 - i, "2026-09-20T10:00:00+08:00"))) + "]"
                );
            if (path.StartsWith("/events?status=open&order=DESC&limit=50&offset=50"))
                return (HttpStatusCode.OK, $"[{Summary(150, "2026-08-01T10:00:00+08:00")}]");
            if (path.StartsWith("/events?"))
                return (HttpStatusCode.OK, "[]");
            var id = long.Parse(path["/events/".Length..]);
            return (HttpStatusCode.OK, Detail(id, "open", "2026-09-20T10:00:00+08:00"));
        });
        var client = new SaturdayClient(new HttpClient(handler), "https://api.test", (_, _) => { });

        var tickets = await client.GetActiveTicketsAsync(Since);

        Assert.That(tickets, Has.Count.EqualTo(50));
        Assert.That(handler.Requests, Does.Not.Contain("/events?status=open&order=DESC&limit=50&offset=100"));
    }
}
