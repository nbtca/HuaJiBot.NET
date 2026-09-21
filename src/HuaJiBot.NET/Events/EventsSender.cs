using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Events;

public sealed class EventsSender : IEvents
{
    public event EventHandler? OnStartup;
    public event EventHandler? OnShutdown;
    public event EventHandler? OnInitialized;
    public event EventHandler<BotLoginEventArgs>? OnBotLogin;
    public event EventHandler<GroupMessageEventArgs>? OnGroupMessageReceived;
    public event EventHandler<PrivateMessageEventArgs>? OnPrivateMessageReceived;

    // 公开用于触发事件
    public void CallOnStartup(BotService service) => OnStartup?.Invoke(service, EventArgs.Empty);

    public void CallOnShutdown(BotService service) => OnShutdown?.Invoke(service, EventArgs.Empty);

    public void CallOnInitialized(BotService service) =>
        OnInitialized?.Invoke(service, EventArgs.Empty);

    public void CallOnBotLogin(BotLoginEventArgs e) => OnBotLogin?.Invoke(e.Service, e);

    public void CallOnGroupMessageReceived(GroupMessageEventArgs e) =>
        Raise(OnGroupMessageReceived, e.Service, e);

    public void CallOnPrivateMessageReceived(PrivateMessageEventArgs e) =>
        Raise(OnPrivateMessageReceived, e.Service, e);

    private static void Raise<T>(EventHandler<T>? handler, BotService service, T e)
    {
        if (handler is null)
            return;
        foreach (var h in handler.GetInvocationList().Cast<EventHandler<T>>())
        {
            try
            {
                h(service, e);
            }
            catch (Exception ex)
            {
                service.LogError($"事件处理 {h.Method.DeclaringType?.Name}.{h.Method.Name} 出现异常", ex);
            }
        }
    }
}
