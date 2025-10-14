using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.MQ;

// ReSharper disable once InconsistentNaming
public interface IServerlessMQ : IDisposable
{
    public event Func<JToken, ValueTask>? OnWebhook;
    public event Func<ActiveBroadcastPacketData, ValueTask>? OnClientChanged;
    public event Func<JToken, ValueTask>? OnPacket;
    public event Action<ConnectionInfo>? OnConnected;
    public event Action<DisconnectionInfo>? OnClosed;
    public void Send(string msg);
}

/// <summary>
/// 连接信息
/// </summary>
public class ConnectionInfo
{
    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 是否为重连
    /// </summary>
    public bool IsReconnect { get; set; }
}

/// <summary>
/// 断开连接信息
/// </summary>
public class DisconnectionInfo
{
    /// <summary>
    /// 断开时间
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 断开类型
    /// </summary>
    public DisconnectionType Type { get; set; }

    /// <summary>
    /// 断开原因描述
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 关闭状态码
    /// </summary>
    public int? CloseStatus { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// 断开连接类型
/// </summary>
public enum DisconnectionType
{
    /// <summary>
    /// 未知原因
    /// </summary>
    Unknown,

    /// <summary>
    /// 主动关闭
    /// </summary>
    ByUser,

    /// <summary>
    /// 服务器关闭
    /// </summary>
    ByServer,

    /// <summary>
    /// 网络错误
    /// </summary>
    Error,

    /// <summary>
    /// 连接丢失
    /// </summary>
    Lost,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout,
}
