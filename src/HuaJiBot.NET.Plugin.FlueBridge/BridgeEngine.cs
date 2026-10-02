using System.Text;
using System.Text.Json;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Interfaces;

namespace HuaJiBot.NET.Plugin.FlueBridge;

internal sealed class BridgeEngine(PluginConfig config, BridgeStore store, IPluginService service,
    IReplyQueue queue, Action<string> warn)
{
    internal IngressEvent? Normalize(GroupMessageEventArgs e)
    {
        // A missing account ID must not route a reply to an arbitrary robot.
        if (e.RobotId is not { Length: > 0 } robot || !config.Allows(robot, e.GroupId)
            || e.SenderId == robot || string.IsNullOrWhiteSpace(e.MessageId)) return null;

        var replyReader = e.CommandReader;
        var reply = replyReader.Reply(out var context) ? context : null;
        var mentionReader = e.CommandReader;
        var mentionsBot = mentionReader.At(out var at) && at == robot;
        var conversation = store.Find(robot, e.GroupId, e.MessageId);
        if (conversation is null && reply?.messageId is { Length: > 0 } parent)
            conversation = store.Find(robot, e.GroupId, parent);
        if (conversation is null && !mentionsBot) return null;
        conversation ??= "huajibot:" + string.Join(':', new[] { config.BridgeInstance, robot, e.GroupId, e.MessageId }.Select(Uri.EscapeDataString));

        var textReader = mentionsBot ? mentionReader : replyReader;
        var text = textReader.Input(out var rest, true) ? rest : e.TextMessage;
        if (string.IsNullOrWhiteSpace(text)) return null;
        var eventId = string.Join(':', new[] { config.BridgeInstance, robot, e.GroupId, e.MessageId }.Select(Uri.EscapeDataString));
        var message = new IngressEvent(1, eventId, conversation,
            new Destination(config.BridgeInstance, robot, e.GroupId),
            new IncomingMessage(e.MessageId, e.SenderId, e.SenderMemberCard, text),
            reply?.messageId is { Length: > 0 } id ? new ReplyContext(id, reply.senderId, reply.content) : null);
        if (JsonSerializer.SerializeToUtf8Bytes(message, BridgeJson.Options).Length > config.MaxMessageBytes)
            throw new InvalidDataException("Ingress message exceeds size limit.");
        store.Map(robot, e.GroupId, e.MessageId, conversation);
        return message;
    }

    private bool IsValid(ReplyJob? job)
    {
        if (job is not { Version: 1, Destination: not null, Content: not null }
            || string.IsNullOrWhiteSpace(job.JobId) || job.JobId.Length > 200
            || string.IsNullOrWhiteSpace(job.ConversationId) || job.CreatedAt == default
            || job.Destination.BridgeInstance != config.BridgeInstance
            || !config.Allows(job.Destination.RobotId, job.Destination.GroupId)
            || string.IsNullOrWhiteSpace(job.Destination.ReplyToMessageId)
            || job.Content.Format != "markdown" || string.IsNullOrWhiteSpace(job.Content.Text)
            || Encoding.UTF8.GetByteCount(job.Content.Text) > config.MaxMessageBytes) return false;
        // The Queue credential is not sufficient authority to select any conversation.
        return store.Find(job.Destination.RobotId, job.Destination.GroupId, job.Destination.ReplyToMessageId) == job.ConversationId;
    }

    internal async Task DeliverAsync(QueueLease lease, CancellationToken ct)
    {
        ReplyJob? job = null;
        if (Encoding.UTF8.GetByteCount(lease.Body) <= config.MaxMessageBytes)
        {
            try { job = JsonSerializer.Deserialize<ReplyJob>(lease.Body, BridgeJson.Options); }
            catch (JsonException) { }
        }
        // A sent job remains deduplicated even if its old thread mapping has expired.
        if (job is { Version: 1, Destination: not null } && job.Destination.BridgeInstance == config.BridgeInstance
            && !string.IsNullOrWhiteSpace(job.JobId) && store.WasSent(job.JobId))
        {
            await queue.AckAsync(lease.LeaseId, ct);
            return;
        }
        if (!IsValid(job))
        {
            warn("Discarding malformed or unauthorized Queue job.");
            await queue.AckAsync(lease.LeaseId, ct);
            return;
        }

        try
        {
            ct.ThrowIfCancellationRequested();
            var validated = job!;
            var destination = validated.Destination;
            var content = new RichContent(validated.Content!.Text, destination!.ReplyToMessageId);
            // Do not abandon a non-cancellable adapter send on shutdown: persist its result first.
            var ids = await service.SendRichMessageAsync(destination.RobotId, destination.GroupId, content,
                () => Task.FromResult<SendingMessageBase[]>([content.ToPlainText()]));
            if (ids.Length == 0 || ids.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Adapter returned no delivery receipt.");
            store.Complete(validated, ids);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            warn("Adapter delivery failed; retrying Queue lease.");
            await queue.RetryAsync(lease.LeaseId, ct);
            return;
        }
        // Keep ack failures outside the send catch. A later pull sees the durable sent ledger.
        await queue.AckAsync(lease.LeaseId, ct);
    }
}
