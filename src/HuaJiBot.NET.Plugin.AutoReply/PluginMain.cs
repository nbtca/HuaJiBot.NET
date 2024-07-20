namespace HuaJiBot.NET.Plugin.AutoReply;

public class PluginMain : PluginBase
{
    protected override Task InitializeAsync()
    {
        Service.Log("加载成功");
        Service.Events.OnGroupMessageReceived += Events_OnGroupMessageReceived;
        return base.InitializeAsync();
    }

    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        if (e.TextMessage.Contains("你好"))
        {
            e.Reply("");
        }
    }

    protected override void Unload() { }
}
