using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuaJiBot.NET.Plugin.FlueBridge;

internal sealed class CloudflareQueue(HttpClient http, PluginConfig config, string token) : IReplyQueue
{
    private readonly string _base = $"https://api.cloudflare.com/client/v4/accounts/{Uri.EscapeDataString(config.AccountId)}/queues/{Uri.EscapeDataString(config.QueueId)}/messages/";
    private sealed record Envelope(bool Success, PullResult? Result);
    private sealed record PullResult(List<PulledMessage> Messages);
    private sealed record PulledMessage(string Body, [property: JsonPropertyName("lease_id")] string LeaseId);

    private async Task<Envelope> CallAsync(string action, object payload, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _base + action);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Queue API request failed.", null, response.StatusCode);
        var result = JsonSerializer.Deserialize<Envelope>(await response.Content.ReadAsStringAsync(ct), BridgeJson.Options);
        if (result?.Success != true) throw new HttpRequestException("Queue API reported failure.");
        return result;
    }

    public async Task<IReadOnlyList<QueueLease>> PullAsync(CancellationToken ct)
    {
        // Process one lease at a time so adapter rendering cannot expire later leases in a batch.
        var envelope = await CallAsync("pull", new { batch_size = 1, visibility_timeout = config.VisibilityTimeoutSeconds * 1000 }, ct);
        if (envelope.Result?.Messages is null) throw new InvalidDataException("Invalid Queue pull response.");
        return envelope.Result.Messages.Select(m => new QueueLease(m.Body, m.LeaseId)).ToArray();
    }

    public async Task AckAsync(string leaseId, CancellationToken ct) =>
        await CallAsync("ack", new { acks = new[] { new { lease_id = leaseId } }, retries = Array.Empty<object>() }, ct);

    public async Task RetryAsync(string leaseId, CancellationToken ct) =>
        await CallAsync("ack", new { acks = Array.Empty<object>(), retries = new[] { new { lease_id = leaseId, delay_seconds = config.RetryDelaySeconds } } }, ct);
}
