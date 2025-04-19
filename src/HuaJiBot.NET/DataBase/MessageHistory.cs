using HuaJiBot.NET.Bot;
using LiteDB;

namespace HuaJiBot.NET.DataBase;

public class GroupMessage
{
    [BsonId]
    public required string MessageId { get; set; }
    public required string GroupId { get; set; }
    public required string? SenderId { get; set; } //null表示机器人自己
    public required string SenderName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public required string Content { get; set; }
    public required bool IsBot { get; set; } = false;
    public required string? ReplyToMessageId { get; set; } = null;
}

public class MessageHistory : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<GroupMessage> _messages;
    private bool _disposed = false;
    private BotService _service;

    public MessageHistory(BotService service, string dbName = "messages.db")
    {
        _service = service;
        var dbPath = Path.Combine(service.GetPluginDataPath(), "database", dbName);
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _db = new LiteDatabase(dbPath);
        _messages = _db.GetCollection<GroupMessage>("messages");

        // Create indices for faster querying
        _messages.EnsureIndex(x => x.GroupId);
        _messages.EnsureIndex(x => x.SenderId);
        _messages.EnsureIndex(x => x.Timestamp);
    }

    public void StoreMessage(GroupMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        _messages.Insert(message);
        _service.LogDebug($"[LiteDB] [+] {message.MessageId}: {message.Content}");
    }

    public GroupMessage? GetMessage(string messageId)
    {
        return _messages.FindOne(x => x.MessageId == messageId);
    }

    public IEnumerable<GroupMessage> GetGroupMessages(string groupId, int limit = 100, int skip = 0)
    {
        return _messages.Find(x => x.GroupId == groupId, skip, limit);
    }

    public GroupMessage? GetGroupMessageLastEndWith(string groupId, string text)
    {
        var messages =
            from m in _messages.FindAll()
            orderby m.Timestamp descending
            where m.GroupId == groupId && m.Content.EndsWith(text)
            select m;
        return messages.FirstOrDefault();
    }

    /// <summary>
    /// Levenshtein 距离用于衡量两个字符串之间的差异，常用于实现模糊匹配。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    private static int LevenshteinDistance(string source, string target)
    {
        var n = source.Length;
        var m = target.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; d[i, 0] = i++)
            ;
        for (var j = 0; j <= m; d[0, j] = j++)
            ;
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }
        return d[n, m];
    }

    public GroupMessage? GetGroupMessageLastSimilar(string groupId, string text)
    {
        var messages =
            from m in _messages.FindAll()
            orderby m.Timestamp descending
            where m.GroupId == groupId && LevenshteinDistance(m.Content, text) <= text.Length / 10 //差异小于10%
            select m;
        return messages.FirstOrDefault();
    }

    public IEnumerable<GroupMessage> GetUserMessages(string userId, int limit = 100, int skip = 0)
    {
        return _messages.Find(x => x.SenderId == userId, skip, limit);
    }

    public IEnumerable<GroupMessage> GetUserGroupMessages(
        string userId,
        string groupId,
        int limit = 100,
        int skip = 0
    )
    {
        return _messages.Find(x => x.SenderId == userId && x.GroupId == groupId, skip, limit);
    }

    public IEnumerable<GroupMessage> GetMessagesByTimeRange(
        DateTime start,
        DateTime end,
        int limit = 100,
        int skip = 0
    )
    {
        return _messages.Find(x => x.Timestamp >= start && x.Timestamp <= end, skip, limit);
    }

    public void DeleteMessage(string messageId)
    {
        _messages.DeleteMany(x => x.MessageId == messageId);
    }

    public bool UpdateMessage(GroupMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return _messages.Update(message);
    }

    public void ClearGroupHistory(string groupId)
    {
        _messages.DeleteMany(x => x.GroupId == groupId);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _db?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MessageHistory()
    {
        Dispose(false);
    }
}
