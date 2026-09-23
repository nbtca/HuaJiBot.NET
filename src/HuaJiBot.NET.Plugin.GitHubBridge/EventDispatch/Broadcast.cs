using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Plugin.GitHubBridge.Types.Generic;
using HuaJiBot.NET.Utils;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class Broadcast
{
    internal static async Task SendAsync(
        IPluginService service,
        IEnumerable<string> targets,
        params SendingMessageBase[] messages
    )
    {
        foreach (var target in targets)
        {
            try
            {
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

// Each card renders at most once per broadcast, however many targets and platforms use it.
internal sealed class Cards : IDisposable
{
    private readonly List<TempFile.AutoDeleteFile> _files = [];

    internal static Func<Task<T>> Once<T>(Func<Task<T>> build)
    {
        Lazy<Task<T>> value = new(build);
        return () => value.Value;
    }

    internal Func<Task<string>> Card(Func<Task<TempFile.AutoDeleteFile>> build) =>
        Once(async () =>
        {
            var file = await build();
            lock (_files)
                _files.Add(file);
            return (string)file;
        });

    public void Dispose()
    {
        foreach (var file in _files)
            file.Dispose();
    }
}
