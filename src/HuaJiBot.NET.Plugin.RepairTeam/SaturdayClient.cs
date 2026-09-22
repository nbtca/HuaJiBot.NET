using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.RepairTeam;

internal record Ticket(
    long Id,
    string Status,
    string? Model,
    string? Problem,
    string? Member,
    DateTimeOffset Created,
    DateTimeOffset LastActivity
);

internal class SaturdayClient(HttpClient http, string baseUrl, Action<string, Exception> logError)
{
    private static readonly string[] ActiveStatuses = ["open", "accepted", "committed"];
    private const int PageSize = 50;

    private class EventDto
    {
        [JsonProperty("eventId")]
        public long EventId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "";

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("problem")]
        public string? Problem { get; set; }

        [JsonProperty("member")]
        public MemberDto? Member { get; set; }

        [JsonProperty("gmtCreate")]
        public DateTimeOffset GmtCreate { get; set; }

        [JsonProperty("logs")]
        public List<LogDto>? Logs { get; set; }
    }

    private class MemberDto
    {
        [JsonProperty("alias")]
        public string? Alias { get; set; }
    }

    private class LogDto
    {
        [JsonProperty("gmtCreate")]
        public DateTimeOffset GmtCreate { get; set; }
    }

    /// <summary>Tickets in progress that were created at or after <paramref name="since"/>.</summary>
    public async Task<List<Ticket>> GetActiveTicketsAsync(DateTimeOffset since)
    {
        var tickets = new List<Ticket>();
        foreach (var status in ActiveStatuses)
        {
            for (var offset = 0; ; offset += PageSize)
            {
                var page = await GetAsync<List<EventDto>>(
                    $"events?status={status}&order=DESC&limit={PageSize}&offset={offset}"
                );
                var recent = page.TakeWhile(e => e.GmtCreate >= since).ToList();
                foreach (var summary in recent)
                {
                    try
                    {
                        tickets.Add(ToTicket(await GetAsync<EventDto>($"events/{summary.EventId}")));
                    }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException)
                    {
                        logError($"[RepairTeam] 读取工单 #{summary.EventId} 失败", ex);
                    }
                }
                if (recent.Count < PageSize)
                    break;
            }
        }
        return tickets;
    }

    private static Ticket ToTicket(EventDto e) =>
        new(
            e.EventId,
            e.Status,
            e.Model,
            e.Problem,
            e.Member?.Alias,
            e.GmtCreate,
            e.Logs is { Count: > 0 } logs ? logs.Max(l => l.GmtCreate) : e.GmtCreate
        );

    private async Task<T> GetAsync<T>(string path)
    {
        using var resp = await http.GetAsync($"{baseUrl.TrimEnd('/')}/{path}");
        resp.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<T>(await resp.Content.ReadAsStringAsync())
            ?? throw new JsonException($"Empty response from {path}");
    }
}
