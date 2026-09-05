using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class Broadcast
{
    internal static async Task SendAsync(
        BotService service,
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
