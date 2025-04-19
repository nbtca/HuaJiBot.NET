namespace HuaJiBot.NET.Events;

/// <summary>
/// 静态事件
/// </summary>
public interface IEvents
{
    /// <summary>
    /// 服务启动时触发
    /// </summary>
    public event EventHandler? OnStartup;

    /// <summary>
    /// 服务关闭时触发
    /// </summary>
    public event EventHandler? OnShutdown;

    /// <summary>
    /// 服务初始化完成触发
    /// </summary>
    public event EventHandler? OnInitialized;

    public event EventHandler<BotLoginEventArgs>? OnBotLogin;

    public event EventHandler<GroupMessageEventArgs>? OnGroupMessageReceived;

    public event EventHandler<PrivateMessageEventArgs>? OnPrivateMessageReceived;
}
