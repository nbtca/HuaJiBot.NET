using HuaJiBot.NET.Adapter.Satori.Protocol;
using HuaJiBot.NET.Adapter.Satori.Protocol.Elements;
using HuaJiBot.NET.Adapter.Satori.Protocol.Models;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Adapter.Satori;

public class SatoriAdapter : BotServiceBase
{
    private SatoriApiClient _apiClient;
    private SatoriEventClient _eventClient;
    internal string[] Accounts = [];
    public string PlatformId = "";
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

    public override string[] AllRobots => Accounts;

    private string ConvertFileToBase64(string path)
    {
        var mineType = "image/png";
        if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
            mineType = "image/jpeg";
        else if (path.EndsWith(".gif"))
            mineType = "image/gif";
        else if (path.EndsWith(".bmp"))
            mineType = "image/bmp";
        else if (path.EndsWith(".webp"))
            mineType = "image/webp";
        return "data:" + mineType + ";base64," + Convert.ToBase64String(File.ReadAllBytes(path));
    }

    public override void SendGroupMessage(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        var robots = robotId is null ? AllRobots : [robotId];
        foreach (var robot in robots)
            _ = _apiClient.SendGroupMessageAsync(
                robot,
                targetGroup,
                (
                    from x in messages
                    select (Element)(
                        x switch
                        {
                            AtMessage { Target: var target } => new AtElement { Id = target },
                            ImageMessage { ImagePath: var path }
                                => new ImageElement { Src = ConvertFileToBase64(path) },
                            ReplyMessage { Target: var target, ReplyMsgId: var msgId }
                                => new QuoteElement { Id = msgId },
                            TextMessage { Text: var text } => new TextElement { Text = text },
                            _ => throw new ArgumentOutOfRangeException(nameof(x)),
                        }
                    )
                ).ToArray()
            );
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
        var path = Path.GetFullPath(Path.Combine("plugins", "data")); //插件数据目录，当前目录下的plugins/data
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path); //自动创建目录
        return path;
    }
}
