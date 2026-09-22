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
