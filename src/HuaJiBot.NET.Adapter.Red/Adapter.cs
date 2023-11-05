using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Adapter.Red;

public class RedProtocolAdapter : BotServiceBase
{
    public RedProtocolAdapter(string url, string token)
    {
        _connector = new(this, url);
        _token = token;
    }

    readonly Connector _connector;
    readonly string _token;

    public override async Task SetupService()
    {
        await _connector.Connect(_token);
    }

    public override string[] GetAllRobots()
    {
        throw new NotImplementedException();
    }

    public override void SendGroupMessage(string robotId, string targetGroup, string message)
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string robotId, string targetGroup, string userId, string text)
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

    /// <summary>
    /// 日志
    /// </summary>
    /// <param name="message">日志内容</param>
    public override void Log(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}");
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    /// <param name="message"></param>
    public override void Warn(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}");
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    /// <param name="message">调试日志内容</param>
    public override void LogDebug(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] {message}");
    }

    /// <summary>
    /// 错误日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="detail">错误信息</param>
    public override void LogError(object message, object detail)
    {
        Console.WriteLine(
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}"
                + $"{Environment.NewLine}---{Environment.NewLine}"
                + $"{detail}"
                + $"{Environment.NewLine}---"
        );
    }

    /// <summary>
    /// 取插件数据目录
    /// </summary>
    /// <returns></returns>
    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data")); //插件数据目录，当前目录下的plugins/data
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path); //自动创建目录
        return path;
    }
}
