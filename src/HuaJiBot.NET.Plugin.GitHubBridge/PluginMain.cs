using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Plugin.TelegramBridge;

public class PluginConfig : ConfigBase
{
    public string Test { get; set; } = "test";
}
public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    public PluginConfig Config { get; } = new();
    protected override Task Initialize()
    {
        Service.Events.OnGroupMessageReceived += Events_OnGroupMessageReceived;
        Service.Log("[1] 启动成功！");
        Service.Log("Config " + Config.Test);
        return Task.CompletedTask;
    }

    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        if (e.TextMessage.StartsWith("testimg"))
        {
            Service.SendGroupMessage(null, e.GroupId, new SendImageInfo
            {
                ImagePath = "C:\\Users\\Administrator\\Desktop\\test.png"
            });
        }
    }
    protected override void Unload() { }
}
