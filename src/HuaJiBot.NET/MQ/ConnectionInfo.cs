namespace HuaJiBot.NET.MQ;

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