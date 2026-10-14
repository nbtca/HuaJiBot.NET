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
        SeedSummaryAnchors(robot, e.GroupId);

        var replyReader = e.CommandReader;
        var reply = replyReader.Reply(out var context) ? context : null;
        var mentionReader = e.CommandReader;
        var mentionsBot = mentionReader.At(out var at) && at == robot;
        var conversation = store.Find(robot, e.GroupId, e.MessageId);
        if (conversation is null && reply?.messageId is { Length: > 0 } parent)
            conversation = store.Find(robot, e.GroupId, parent);
        var chat = conversation is not null || mentionsBot;
        conversation ??= "huajibot:" + string.Join(':', new[] { config.BridgeInstance, robot, e.GroupId, e.MessageId }.Select(Uri.EscapeDataString));

        var textReader = mentionsBot ? mentionReader : replyReader;
        var text = chat && textReader.Input(out var rest, true) ? rest : e.TextMessage;
        if (string.IsNullOrWhiteSpace(text)) text = e.ImageUrls.Length > 0 ? "请描述这张图片。" : "（非文本消息）";
        var summary = System.Text.RegularExpressions.Regex.IsMatch(text.Trim(), @"^(总结|summary)(\s*[今昨前]\s*[天日])?$");
        var kind = summary ? "summary" : chat ? "chat" : "archive";
        var eventId = string.Join(':', new[] { config.BridgeInstance, robot, e.GroupId, e.MessageId }.Select(Uri.EscapeDataString));
        var message = new IngressEvent(1, eventId, conversation,
            new Destination(config.BridgeInstance, robot, e.GroupId),
            new IncomingMessage(e.MessageId, e.SenderId, e.SenderMemberCard, text,
                kind == "chat" ? e.ImageUrls : [], DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            reply?.messageId is { Length: > 0 } id ? new ReplyContext(id, reply.senderId, reply.content) : null, kind);
        if (JsonSerializer.SerializeToUtf8Bytes(message, BridgeJson.Options).Length > config.MaxMessageBytes)
            throw new InvalidDataException("Ingress message exceeds size limit.");
        if (kind != "archive") store.Map(robot, e.GroupId, e.MessageId, conversation);
        return message;
    }

    internal void SeedSummaryAnchors(string robot, string group)
    {
        for (var days = 0; days < 3; days++)
        {
            var day = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).AddDays(-days).ToString("yyyy-MM-dd");
            var anchor = "summary:" + day;
            var conversation = "huajibot:" + string.Join(':', new[] { config.BridgeInstance, robot, group }.Select(Uri.EscapeDataString)) + ":" + anchor;
            store.Map(robot, group, anchor, conversation);
        }
    }

    private bool IsValid(ReplyJob? job)
    {
        if (job is not { Version: 1, Destination: not null, Content: not null }
            || string.IsNullOrWhiteSpace(job.JobId) || job.JobId.Length > 200
            || string.IsNullOrWhiteSpace(job.ConversationId) || job.CreatedAt == default
            || job.Destination.BridgeInstance != config.BridgeInstance
            || !config.Allows(job.Destination.RobotId, job.Destination.GroupId)
            || string.IsNullOrWhiteSpace(job.Destination.ReplyToMessageId)
            || job.Kind is not ("chat" or "summary")
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
            var content = new RichContent(validated.Content!.Text,
                validated.Kind == "summary" && destination!.ReplyToMessageId.StartsWith("summary:", StringComparison.Ordinal)
                    ? null : destination!.ReplyToMessageId);
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
