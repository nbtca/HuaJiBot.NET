using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;
using Kook;
using Kook.WebSocket;

namespace HuaJiBot.NET.Adapter.Kook;

public class KookAdapter : BotServiceBase
{
    private readonly KookSocketClient _client;
    private readonly string _token;

    public KookAdapter(string token)
    {
        _token = token;
        _client = new KookSocketClient();
    }

    public override required ILogger Logger { get; init; }

    public override void Reconnect()
    {
        _ = Task.Run(async () =>
        {
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                await _client.StopAsync();
            }
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        });
    }

    public override async Task SetupServiceAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        
        _client.MessageReceived += OnMessageReceived;
        _client.Ready += OnReady;
    }

    private Task OnReady()
    {
        Events.CallOnBotLogin(this, new Events.BotLoginEventArgs
        {
            BotId = _client.CurrentUser?.Id.ToString() ?? "",
            BotName = _client.CurrentUser?.Username ?? "",
            ClientVersion = "Kook.Net"
        });
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(Cacheable<IMessage, Guid> arg1, ISocketMessageChannel arg2)
    {
        // TODO: Implement message handling
        return Task.CompletedTask;
    }

    public override string[] AllRobots => _client.CurrentUser is not null 
        ? [_client.CurrentUser.Id.ToString()] 
        : [];

    public override async Task<string[]> SendGroupMessageAsync(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        // TODO: Implement group message sending
        throw new NotImplementedException();
    }

    public override void RecallMessage(string? robotId, string targetGroup, string msgId)
    {
        // TODO: Implement message recall
        throw new NotImplementedException();
    }

    public override void SetGroupName(string? robotId, string targetGroup, string groupName)
    {
        // TODO: Implement group name setting
        throw new NotImplementedException();
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        // TODO: Implement member type retrieval
        return MemberType.Unknown;
    }

    public override async Task<string[]> FeedbackAt(
        string? robotId,
        string targetGroup,
        string msgId,
        string text
    )
    {
        // TODO: Implement feedback at functionality
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        // TODO: Implement nickname retrieval
        return "";
    }

    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data"));
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
}