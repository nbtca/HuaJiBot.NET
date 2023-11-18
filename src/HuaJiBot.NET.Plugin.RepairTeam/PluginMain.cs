using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.RepairTeam;

public class PluginMain : PluginBase
{
    protected override Task Initialize()
    {
        Service.Events.OnGroupMessageReceived += Events_OnGroupMessageReceived;
        Service.Log("[1] 启动成功！");
        return Task.CompletedTask;
    }
    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        if (e.TextMessage.StartsWith("test000"))
        {
            Console.WriteLine(JsonConvert.SerializeObject(e));
            e.Feedback("test");
        }

        Service.Log($"[{e.GroupName}] <{e.SenderMemberCard}> {e.TextMessage}");
    }
    protected override void Unload() { }
}
