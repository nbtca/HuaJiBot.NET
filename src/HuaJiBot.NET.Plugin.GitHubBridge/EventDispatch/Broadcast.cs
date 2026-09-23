using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class Broadcast
{
    internal static async Task SendAsync(
        IPluginService service,
        IEnumerable<string> targets,
        Func<Task<SendingMessageBase[]>> build
    )
    {
        SendingMessageBase[]? messages = null;
        foreach (var target in targets)
        {
            try
            {
                messages ??= await build();
                await service.SendGroupMessageAsync(null, target, messages);
            }
            catch (Exception e)
            {
                service.LogError($"推送到 {target} 失败", e);
            }
        }
    }

    internal static bool IsBot(Sender sender) =>
        sender.Type == "Bot" || sender.Login.EndsWith("[bot]");
}
