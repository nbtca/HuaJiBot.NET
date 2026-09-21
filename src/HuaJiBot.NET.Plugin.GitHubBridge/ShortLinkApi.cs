using System.Net.Http.Json;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Plugin.GitHubBridge;

internal class ShortLinkApi(string token)
{
    public record ShortLinkResult
    {
        [JsonProperty("original")]
        public required string Original { get; init; }

        [JsonProperty("url")]
        public required string Url { get; init; }
    }

    private static readonly HttpClient Client = new();

    public async Task<ShortLinkResult> ShortLinkAsync(
        string apiUrl,
        string url,
        string? path = null
    )
    {
        var obj = new JObject { ["url"] = url };
        if (path is not null)
            obj["path"] = path;
        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(obj.ToString(Formatting.None), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Authorization", $"Bearer {token}");
        using var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShortLinkResult>()
            ?? throw new NullReferenceException("Response is empty");
    }
}
