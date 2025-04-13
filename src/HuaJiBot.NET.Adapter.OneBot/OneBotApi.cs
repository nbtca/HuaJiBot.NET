using System.Collections.Concurrent;
using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Bot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot;

internal class OneBotApi(BotService service, Action<string> send)
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

#if DEBUGs
        Console.WriteLine("Sending: " + str);
#endif
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
                    _ => new Exception("Unknown error: " + ret.Status),
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

    private Task SendAsync<T>(string action, T data) => SendAsync<T, JToken>(action, data);

    public Task ProcessMessageAsync(JObject data)
    {
        var echo = data.Value<string>("echo");
        if (echo is null)
        {
            service.LogDebug("Invalid message: " + data);
            return Task.CompletedTask;
        }
        if (_pendingRequests.TryRemove(echo, out var tcs))
            tcs.TrySetResult(data);
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>> GetVersionInfoAsync()
    {
        return SendAsync<Dictionary<string, string>>("get_version_info");
    }

    public class OneBotGetGroupInfo
    {
        [JsonProperty("group_id")]
        public required uint GroupId { get; set; }

        [JsonProperty("no_cache")]
        public bool NoCache { get; set; }
    }

    public class OneBotGroup
    {
        [JsonProperty("group_id")]
        public required uint GroupId { get; set; }

        [JsonProperty("group_name")]
        public required string GroupName { get; set; }

        [JsonProperty("member_count")]
        public required uint MemberCount { get; set; }

        [JsonProperty("max_member_count")]
        public required uint MaxMemberCount { get; set; }
    }

    public async Task<string> GetGroupNameAsync(string groupId)
    {
        var result = await SendAsync<OneBotGetGroupInfo, OneBotGroup>(
            "get_group_info",
            new OneBotGetGroupInfo { GroupId = uint.Parse(groupId), NoCache = false }
        );
        return result.GroupName;
    }

    public class OneBotGroupMessageBase
    {
        [JsonProperty("group_id")]
        public uint GroupId { get; set; }

        [JsonProperty("auto_escape")]
        public bool? AutoEscape { get; set; }
    }

    public class OneBotMessageResponse
    {
        [JsonProperty("message_id")]
        public required int MessageId { get; set; }
    }

    public class OneBotMessage : OneBotGroupMessageBase
    {
        [JsonProperty("message")]
        public MessageEntity[] Messages { get; set; } = [];
    }

    public class OneBotMessageSimple(MessageEntity entity) : OneBotGroupMessageBase
    {
        [JsonProperty("message")]
        public MessageEntity Messages { get; set; } = entity;
    }

    public class OneBotMessageText : OneBotGroupMessageBase
    {
        [JsonProperty("message")]
        public string Messages { get; set; } = "";
    }

    public Task<OneBotMessageResponse> SendGroupMessageAsync(string targetGroup, string message)
    {
        return SendAsync<OneBotMessageText, OneBotMessageResponse>(
            "send_group_msg",
            new OneBotMessageText
            {
                AutoEscape = true,
                GroupId = uint.Parse(targetGroup),
                Messages = message,
            }
        );
    }

    public async Task<OneBotMessageResponse> SendGroupMessageAsync(
        string targetGroup,
        params MessageEntity[] messages
    )
    {
        if (messages is [var entity])
        {
            var msg = new OneBotMessageSimple(entity) { GroupId = uint.Parse(targetGroup) };
            return await SendAsync<OneBotMessageSimple, OneBotMessageResponse>(
                "send_group_msg",
                msg
            );
        }
        else
        {
            var msg = new OneBotMessage { GroupId = uint.Parse(targetGroup), Messages = messages };
            return await SendAsync<OneBotMessage, OneBotMessageResponse>("send_group_msg", msg);
        }
    }

    public class OneBotDeleteMsg
    {
        [JsonProperty("message_id")]
        public required int MessageId { get; set; }
    }

    public Task RecallMessageAsync(string targetGroup, string messageId)
    {
        var msg = new OneBotDeleteMsg { MessageId = int.Parse(messageId) };
        return SendAsync("delete_msg", msg);
    }

    public class OneBotSetGroupName
    {
        [JsonProperty("group_id")]
        public required string GroupId { get; set; }

        [JsonProperty("group_name")]
        public required string GroupName { get; set; }
    }

    public Task SetGroupNameAsync(string groupId, string groupName)
    {
        var msg = new OneBotSetGroupName { GroupId = groupId, GroupName = groupName };
        return SendAsync("set_group_name", msg);
    }
}
