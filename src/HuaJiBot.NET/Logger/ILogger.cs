namespace HuaJiBot.NET.Logger;

public interface ILogger
{
    /// <summary>
    /// 日志
    /// </summary>
    /// <param name="message">日志内容</param>
    void Log(object message);

    /// <summary>
    /// 警告日志
    /// </summary>
    /// <param name="message"></param>
    void Warn(object message);

    /// <summary>
    /// 调试日志
    /// </summary>
    /// <param name="message">调试日志内容</param>
    void LogDebug(object message);

    /// <summary>
    /// 错误日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="detail">错误信息</param>
    void LogError(object message, object detail);
}
