using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HuaJiBot.NET.Adapter.Satori.Protocol.Elements;
using HuaJiBot.NET.Adapter.Satori.Protocol.Models;
using HuaJiBot.NET.Bot;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.Satori.Protocol;

internal class SatoriApiClient
{
    private readonly HttpClient _http;
    private readonly SatoriAdapter _service;

    public SatoriApiClient(SatoriAdapter service, Uri httpUrl, string token)
    {
        _service = service;
        _http = new HttpClient { BaseAddress = httpUrl };
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public Task<Message[]> SendGroupMessageAsync(
        string selfId,
        string channelId,
        params Element[] contents
    )
    {
        var sb = new StringBuilder();
        foreach (var element in contents)
            sb.Append(ElementSerializer.Serialize(element));
        return HttpPostAsync<Message[]>(
            selfId,
            "/v1/message.create",
            new JObject { ["channel_id"] = channelId, ["content"] = sb.ToString() }
        );
    }

    public async Task<TData> HttpPostAsync<TData>(string selfId, string endpoint, JObject? body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("X-Platform", _service.PlatformId);
        request.Headers.Add("X-Self-ID", selfId);
        request.Content = JsonContent.Create(body);
        var response = await _http.SendAsync(request);
        var data = await response.Content.ReadFromJsonAsync<TData>(JsonOptions);
        return data!;
    }

    internal static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
