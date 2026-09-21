using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Interfaces;

public static class PluginServiceExtensions
{
    public static async Task<string[]> TrySendGroupMessageAsync(
        this IPluginService service,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        try
        {
            return await service.SendGroupMessageAsync(null, targetGroup, messages);
        }
        catch (Exception e)
        {
            service.LogError($"发送消息到群 {targetGroup} 失败", e);
            return [];
        }
    }
}
