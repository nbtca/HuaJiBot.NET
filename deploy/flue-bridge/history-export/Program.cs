using LiteDB;
using System.Text.Json;

if (args.Length != 4) throw new ArgumentException("database, output, robot, group required");
using var db = new LiteDatabase(new ConnectionString { Filename = args[0], ReadOnly = true });
using var output = new StreamWriter(new FileStream(args[1], FileMode.CreateNew));
var messages = db.GetCollection("messages").FindAll().Where(m => m["GroupId"].AsString == args[3])
    .OrderBy(m => m["Timestamp"].AsDateTime);
var count = 0;
foreach (var message in messages)
{
    var observed = new DateTimeOffset(message["Timestamp"].AsDateTime.ToUniversalTime());
    if (observed < DateTimeOffset.UtcNow.AddDays(-3)) continue;
    var id = message["_id"].AsString;
    var components = new[] { "main", args[2], args[3], id }.Select(Uri.EscapeDataString);
    var text = message["Content"].IsString ? message["Content"].AsString : "（非文本消息）";
    if (string.IsNullOrWhiteSpace(text)) text = "（非文本消息）";
    var payload = new { version = 1, eventId = string.Join(':', components), conversationId = "huajibot:" + string.Join(':', components),
        destination = new { bridgeInstance = "main", robotId = args[2], groupId = args[3] }, kind = "archive",
        message = new { messageId = id, senderId = message["SenderId"].IsString ? message["SenderId"].AsString : "bot",
            senderName = message["SenderName"].AsString, text, images = Array.Empty<string>(), observedAt = observed.ToUnixTimeMilliseconds() }, replyTo = (object?)null };
    output.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload)); count++;
}
Console.WriteLine($"Exported {count} history records; original database opened read-only.");
