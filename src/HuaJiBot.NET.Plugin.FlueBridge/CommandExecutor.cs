using System.Security.Cryptography;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.Plugin.FlueBridge;

internal sealed class CommandExecutor(BridgeStore store, BotService service)
{
    internal async Task ExecuteAsync(CommandJob job, CancellationToken ct)
    {
        var origin = store.CommandOrigin(job.Destination);
        if (origin is not { } sender || sender.SenderId != job.SenderId)
            throw new InvalidOperationException("Command sender does not match the locally recorded event.");

        var e = new GroupMessageEventArgs(() => new DefaultCommandReader([]), () => ValueTask.FromResult(string.Empty))
        {
            Service = service,
            RobotId = job.Destination.RobotId,
            MessageId = job.Destination.ReplyToMessageId,
            GroupId = job.Destination.GroupId,
            SenderId = sender.SenderId,
            SenderMemberCard = sender.SenderName,
            SenderMemberType = sender.Role,
            TextMessageLazy = new(() => string.Empty),
            ReplyHandler = (key, send) => SendReplyAsync(store, job, key, send),
        };
        await service.CommandService.ExecuteCommandAsync(job.Command.Id, job.Command.Arguments, e, ct);
    }

    internal static async Task<string[]> SendReplyAsync(BridgeStore store, CommandJob job, string key, Func<Task<string[]>> send)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        if (store.CommandReply(job.JobId, hash) is { } receipt) return receipt;
        // An adapter send is not cancellable. Always persist its receipt before observing shutdown.
        var ids = await send();
        if (ids.Length == 0 || ids.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Adapter returned no command delivery receipt.");
        store.SaveCommandReply(job, hash, ids);
        return ids;
    }
}
