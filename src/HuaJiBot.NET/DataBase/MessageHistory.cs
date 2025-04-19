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
}

public class MessageHistory : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<GroupMessage> _messages;
    private bool _disposed = false;

    public MessageHistory(BotService service, string dbName = "messages.db")
    {
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
    }

    public GroupMessage GetMessage(string messageId)
    {
        return _messages.FindById(messageId);
    }

    public IEnumerable<GroupMessage> GetGroupMessages(string groupId, int limit = 100, int skip = 0)
    {
        return _messages.Find(x => x.GroupId == groupId, skip, limit);
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
        _messages.Delete(messageId);
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
