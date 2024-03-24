using HuaJiBot.NET.Adapter.Satori.Protocol;
using HuaJiBot.NET.Adapter.Satori.Protocol.Models;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Adapter.Satori;

public class SatoriAdapter : BotServiceBase
{
    private SatoriApiClient _apiClient;
    private SatoriEventClient _eventClient;
    internal Login[] Accounts = Array.Empty<Login>();
    public override required ILogger Logger { get; init; }

    public SatoriAdapter(string url, string token)
    {
        var baseUri = new Uri(url);
        var httpUrl = baseUri.Scheme is "ws" or "wss"
            ? new UriBuilder(baseUri) { Scheme = baseUri.Scheme == "ws" ? "http" : "https" }.Uri
            : baseUri;
        var wsUrl = new Uri(
            new UriBuilder(httpUrl) { Scheme = baseUri.Scheme == "http" ? "ws" : "wss" }.Uri,
            new Uri("/v1/events", UriKind.Relative)
        );
        _apiClient = new SatoriApiClient(this, httpUrl, token);
        _eventClient = new SatoriEventClient(this, wsUrl, token);
    }

    public override void Reconnect() => _ = _eventClient.ConnectAsync();

    public override Task SetupServiceAsync() => _eventClient.ConnectAsync();

    public override string[] GetAllRobots()
    {
        return (
            from x in Accounts
            where x is { Status: Status.Online, User: not null }
            select x.User!.Id
        ).ToArray();
    }

    public override void SendGroupMessage(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string? robotId, string targetGroup, string userId, string text)
    {
        throw new NotImplementedException();
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetPluginDataPath()
    {
        throw new NotImplementedException();
    }
}
