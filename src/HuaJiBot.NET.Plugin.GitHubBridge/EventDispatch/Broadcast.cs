using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Interfaces;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class Broadcast
{
    internal static async Task SendAsync(
        IPluginService service,
        IEnumerable<string> targets,
        RichContent content,
        Func<Task<SendingMessageBase[]>> fallback
    )
    {
        Task<SendingMessageBase[]>? once = null;
        foreach (var target in targets)
            await service.SendRichMessageAsync(null, target, content, () => once ??= fallback());
    }
}
