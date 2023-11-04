using System;

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

    internal static void CallOnStartup()
    {
        Global.Service.Events.OnStartup?.Invoke(Global.Service, EventArgs.Empty);
    }

    /// <summary>
    /// 服务关闭时触发
    /// </summary>
    public event EventHandler? OnShutdown;

    internal static void CallOnShutdown()
    {
        Global.Service.Events.OnShutdown?.Invoke(Global.Service, EventArgs.Empty);
    }

    /// <summary>
    /// 服务初始化完成触发
    /// </summary>
    public event EventHandler? OnInitialized;

    internal static void CallOnInitialized()
    {
        Global.Service.Events.OnInitialized?.Invoke(Global.Service, EventArgs.Empty);
    }
}
