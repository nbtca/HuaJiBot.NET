namespace HuaJiBot.NET.Websocket;

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