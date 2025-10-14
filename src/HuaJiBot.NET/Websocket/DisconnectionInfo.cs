namespace HuaJiBot.NET.Websocket;

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