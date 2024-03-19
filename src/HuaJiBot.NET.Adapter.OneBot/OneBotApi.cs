using System.Collections.Concurrent;
using HuaJiBot.NET.Bot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot;

internal class OneBotApi(BotServiceBase service, Action<string> send)
{
    private class ActionRequest<T>(string action, T? data, string echo)
    {
        [JsonProperty("action")]
        public string Action { get; init; } = action;

        [JsonProperty("params")]
        public T? Data { get; init; } = data;

        [JsonProperty("echo")]
        public string Echo { get; init; } = echo;
    }

    private record ActionResponse<T>(
        [JsonProperty("status")] string Status,
        [JsonProperty("data")] T Data,
        [JsonProperty("retcode")] int Retcode
    );

    private ConcurrentDictionary<string, TaskCompletionSource<JToken>> _pendingRequests = new();

    private async Task<TR> SendAsync<T, TR>(string action, T data)
    {
        var id = Guid.NewGuid().ToString("N");
        var req = new ActionRequest<T>(action, data, id);
        var str = JsonConvert.SerializeObject(req);
        var tcs = new TaskCompletionSource<JToken>();
        _pendingRequests[id] = tcs;
        try
        {
            send(str);
            var res = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var ret =
                res.ToObject<ActionResponse<TR>>() ?? throw new Exception("Invalid response null");
            if (ret.Status != "ok")
            {
                throw ret.Retcode switch
                {
                    1400 => new Exception("Invalid request"),
                    1404 => new Exception("Action not found"),
                    _ => new Exception("Unknown error: " + ret.Status)
                };
            }
            return ret.Data;
        }
        finally
        {
            _pendingRequests.TryRemove(id, out _);
        }
    }

    private Task<TR> SendAsync<TR>(string action) =>
        SendAsync<JValue, TR>(action, JValue.CreateNull());

    public async Task ProcessMessageAsync(JObject data)
    {
        var echo = data.Value<string>("echo");
        if (echo is null)
        {
            service.LogDebug("Invalid message: " + data);
            return;
        }
        if (_pendingRequests.TryRemove(echo, out var tcs))
            tcs.TrySetResult(data);
    }

    public Task<Dictionary<string, string>> GetVersionInfoAsync()
    {
        return SendAsync<Dictionary<string, string>>("get_version_info");
    }
}
