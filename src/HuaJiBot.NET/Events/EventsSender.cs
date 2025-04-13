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
        OnGroupMessageReceived?.Invoke(e.Service, e);

    public void CallOnPrivateMessageReceived(PrivateMessageEventArgs e) =>
        OnPrivateMessageReceived?.Invoke(e.Service, e);
}
