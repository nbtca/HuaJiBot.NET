using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Events;

/// <summary>
/// 静态事件
/// </summary>
public class Events
{
    /// <summary>
    /// 服务启动时触发
    /// </summary>
    public event EventHandler? OnStartup;

    internal static void CallOnStartup(BotServiceBase service)
    {
        service.Events.OnStartup?.Invoke(service, EventArgs.Empty);
    }

    /// <summary>
    /// 服务关闭时触发
    /// </summary>
    public event EventHandler? OnShutdown;

    internal static void CallOnShutdown(BotServiceBase service)
    {
        service.Events.OnShutdown?.Invoke(service, EventArgs.Empty);
    }

    /// <summary>
    /// 服务初始化完成触发
    /// </summary>
    public event EventHandler? OnInitialized;

    internal static void CallOnInitialized(BotServiceBase service)
    {
        service.Events.OnInitialized?.Invoke(service, EventArgs.Empty);
    }

    public event EventHandler<BotLoginEventArgs>? OnBotLogin;

    public static void CallOnBotLogin(BotLoginEventArgs e)
    {
        var service = e.Service;
        service.Events.OnBotLogin?.Invoke(service, e);
    }

    public event EventHandler<GroupMessageEventArgs>? OnGroupMessageReceived;

    public static void CallOnGroupMessageReceived(GroupMessageEventArgs e)
    {
        var service = e.Service;
        service.Events.OnGroupMessageReceived?.Invoke(service, e);
    }
}
