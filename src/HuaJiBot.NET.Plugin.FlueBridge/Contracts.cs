using System.Text.Json;

namespace HuaJiBot.NET.Plugin.FlueBridge;

internal static class BridgeJson
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal record Destination(string BridgeInstance, string RobotId, string GroupId);
internal record IncomingMessage(string MessageId, string SenderId, string SenderName, string Text);
internal record ReplyContext(string MessageId, string? SenderId, string? Content);
internal record IngressEvent(int Version, string EventId, string ConversationId,
    Destination Destination, IncomingMessage Message, ReplyContext? ReplyTo);
internal record ReplyDestination(string BridgeInstance, string RobotId, string GroupId, string ReplyToMessageId);
internal record ReplyContent(string Format, string Text);
internal record ReplyJob(int Version, string JobId, string ConversationId,
    ReplyDestination Destination, ReplyContent Content, DateTimeOffset CreatedAt);
internal record QueueLease(string Body, string LeaseId);

internal interface IReplyQueue
{
    Task<IReadOnlyList<QueueLease>> PullAsync(CancellationToken ct);
    Task AckAsync(string leaseId, CancellationToken ct);
    Task RetryAsync(string leaseId, CancellationToken ct);
}
