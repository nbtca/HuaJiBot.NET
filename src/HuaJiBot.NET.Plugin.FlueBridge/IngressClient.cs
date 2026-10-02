using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HuaJiBot.NET.Plugin.FlueBridge;

internal sealed class IngressClient(HttpClient http, Uri url, string secret, int maxBytes)
{
    internal static string Sign(string secret, string timestamp, byte[] body)
    {
        var prefix = Encoding.UTF8.GetBytes($"v1:{timestamp}:");
        byte[] signed = [.. prefix, .. body];
        return Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signed));
    }

    internal async Task SendAsync(IngressEvent message, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message, BridgeJson.Options);
        if (body.Length > maxBytes) throw new InvalidDataException("Ingress message exceeds size limit.");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("X-HuaJiBot-Timestamp", timestamp);
        request.Headers.Add("X-HuaJiBot-Signature", $"v1={Sign(secret, timestamp, body)}");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        // Never include a response body, request URL, or credentials in diagnostics.
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Agent ingress rejected the request.", null, response.StatusCode);
    }
}
