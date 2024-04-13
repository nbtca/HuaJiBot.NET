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

    public async Task<ShortLinkResult> ShortLinkAsync(string url, string? path = null)
    {
        using var client = new HttpClient();
        var obj = new JObject { ["url"] = url, };
        if (path is not null)
            obj["path"] = path;
        var content = new StringContent(
            obj.ToString(Formatting.None),
            Encoding.UTF8,
            "application/json"
        );
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        var response = await client.PostAsync("https://link.nbtca.space/api/shorten", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShortLinkResult>()
            ?? throw new NullReferenceException("Response is empty");
    }
}
