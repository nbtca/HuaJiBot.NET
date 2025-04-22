using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HuaJiBot.NET.Adapter.Satori.Protocol.Elements;
using HuaJiBot.NET.Adapter.Satori.Protocol.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HuaJiBot.NET.Adapter.Satori.Protocol;

internal class SatoriApiClient
{
    private readonly HttpClient _http;
    private readonly SatoriAdapter _service;

    //get all api from https://chronocat.vercel.app/openapi.yaml
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

    //https://github.com/satorijs/satori/blob/e285fe65aea7c5d437e365746de82e41bbc06ab7/packages/protocol/src/index.ts#L51
    public Task DeleteMessage(string selfId, string channelId, string messageId)
    {
        return HttpPostAsync<JObject>(
            selfId,
            "/v1/message.delete",
            new JObject { ["channel_id"] = channelId, ["message_id"] = messageId }
        );
    }

    public async Task<JObject> GetMessage(string selfId, string channelId, string messageId)
    {
        return await HttpPostAsync<JObject>(
            selfId,
            "/v1/message.get",
            new JObject { ["channel_id"] = channelId, ["message_id"] = messageId }
        );
    }

    public async Task<JObject> GetMessageList(string selfId, string channelId)
    {
        return await HttpPostAsync<JObject>(
            selfId,
            "/v1/message.list",
            new JObject { ["channel_id"] = channelId }
        );
    }

    public async Task<TData> HttpPostAsync<TData>(string selfId, string endpoint, JObject body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("X-Platform", _service.PlatformId);
        request.Headers.Add("X-Self-ID", selfId);
        request.Content = new StringContent(
            body.ToString(Formatting.None),
            MediaTypeHeaderValue.Parse("application/json")
        );
        var response = await _http.SendAsync(request);
#if DEBUG
        var text = await response.Content.ReadAsStringAsync();
        Console.WriteLine("HttpPostAsync: " + text);
        var data = JsonSerializer.Deserialize<TData>(text, JsonOptions);
#else
        var data = await response.Content.ReadFromJsonAsync<TData>(JsonOptions);
#endif
        return data!;
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
