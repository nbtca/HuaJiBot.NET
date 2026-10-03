using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace HuaJiBot.NET.Plugin.FlueBridge;

internal sealed class BridgeStore : IDisposable
{
    private readonly SqliteConnection _db;
    private readonly object _gate = new();
    private static readonly Lazy<bool> NativeResolver = new(() =>
    {
        var provider = typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly;
        NativeLibrary.SetDllImportResolver(provider, (name, assembly, searchPath) =>
        {
            if (name != "e_sqlite3") return IntPtr.Zero;
            var filename = OperatingSystem.IsWindows() ? "e_sqlite3.dll"
                : OperatingSystem.IsMacOS() ? "libe_sqlite3.dylib" : "libe_sqlite3.so";
            var root = Path.GetDirectoryName(assembly.Location)!;
            var path = Path.Combine(root, "runtimes", RuntimeInformation.RuntimeIdentifier, "native", filename);
            return File.Exists(path) ? NativeLibrary.Load(path) : IntPtr.Zero;
        });
        return true;
    });

    internal BridgeStore(string path)
    {
        _ = NativeResolver.Value;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        _db.Open();
        using var command = _db.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=FULL;
            CREATE TABLE IF NOT EXISTS message_conversations (
                robot_id TEXT NOT NULL, group_id TEXT NOT NULL, message_id TEXT NOT NULL,
                conversation_id TEXT NOT NULL, created_at INTEGER NOT NULL,
                PRIMARY KEY (robot_id, group_id, message_id));
            CREATE TABLE IF NOT EXISTS queue_deliveries (
                job_id TEXT PRIMARY KEY, status TEXT NOT NULL, conversation_id TEXT NOT NULL,
                resulting_message_ids TEXT NOT NULL, updated_at INTEGER NOT NULL);
            CREATE INDEX IF NOT EXISTS mapping_age ON message_conversations(created_at);
            CREATE INDEX IF NOT EXISTS delivery_age ON queue_deliveries(updated_at);
            CREATE TABLE IF NOT EXISTS ingress_outbox (event_id TEXT PRIMARY KEY, body TEXT NOT NULL, created_at INTEGER NOT NULL);
            """;
        command.ExecuteNonQuery();
    }

    internal string? Find(string robot, string group, string message)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT conversation_id FROM message_conversations WHERE robot_id=$r AND group_id=$g AND message_id=$m";
            cmd.Parameters.AddWithValue("$r", robot);
            cmd.Parameters.AddWithValue("$g", group);
            cmd.Parameters.AddWithValue("$m", message);
            return cmd.ExecuteScalar() as string;
        }
    }

    private void MapCore(string robot, string group, string message, string conversation, SqliteTransaction? tx)
    {
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO message_conversations VALUES ($r,$g,$m,$c,$t)
            ON CONFLICT(robot_id,group_id,message_id) DO UPDATE SET created_at=excluded.created_at
            """;
        cmd.Parameters.AddWithValue("$r", robot);
        cmd.Parameters.AddWithValue("$g", group);
        cmd.Parameters.AddWithValue("$m", message);
        cmd.Parameters.AddWithValue("$c", conversation);
        cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        cmd.ExecuteNonQuery();
    }

    internal void Map(string robot, string group, string message, string conversation)
    {
        lock (_gate) MapCore(robot, group, message, conversation, null);
    }

    internal void SaveIngress(IngressEvent message)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO ingress_outbox VALUES($id,$body,$t)";
            cmd.Parameters.AddWithValue("$id", message.EventId);
            cmd.Parameters.AddWithValue("$body", JsonSerializer.Serialize(message, BridgeJson.Options));
            cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }
    }

    internal IngressEvent[] PendingIngress()
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT body FROM ingress_outbox ORDER BY created_at,rowid LIMIT 50";
            using var reader = cmd.ExecuteReader();
            var messages = new List<IngressEvent>();
            while (reader.Read()) messages.Add(JsonSerializer.Deserialize<IngressEvent>(reader.GetString(0), BridgeJson.Options)!);
            return messages.ToArray();
        }
    }

    internal void RemoveIngress(string id)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "DELETE FROM ingress_outbox WHERE event_id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
    }

    internal bool WasSent(string job)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM queue_deliveries WHERE job_id=$j AND status='sent'";
            cmd.Parameters.AddWithValue("$j", job);
            return cmd.ExecuteScalar() is not null;
        }
    }

    internal void Complete(ReplyJob job, string[] ids)
    {
        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            foreach (var id in ids)
                MapCore(job.Destination.RobotId, job.Destination.GroupId, id, job.ConversationId, tx);
            using var cmd = _db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO queue_deliveries VALUES ($j,'sent',$c,$i,$t) ON CONFLICT(job_id) DO NOTHING";
            cmd.Parameters.AddWithValue("$j", job.JobId);
            cmd.Parameters.AddWithValue("$c", job.ConversationId);
            cmd.Parameters.AddWithValue("$i", JsonSerializer.Serialize(ids));
            cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
            tx.Commit();
        }
    }

    internal void Prune(DateTimeOffset before)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "DELETE FROM message_conversations WHERE created_at<$t; DELETE FROM queue_deliveries WHERE status='sent' AND updated_at<$t";
            cmd.Parameters.AddWithValue("$t", before.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose() { lock (_gate) _db.Dispose(); }
}
