using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Runtime.InteropServices;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;

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
            CREATE TABLE IF NOT EXISTS ingress_failures (
                event_id TEXT PRIMARY KEY, status_code INTEGER, attempts INTEGER NOT NULL,
                next_attempt_at INTEGER NOT NULL, rejected_at INTEGER);
            CREATE TABLE IF NOT EXISTS command_origins (
                robot_id TEXT NOT NULL, group_id TEXT NOT NULL, message_id TEXT NOT NULL,
                sender_id TEXT NOT NULL, sender_name TEXT NOT NULL, member_type INTEGER NOT NULL,
                created_at INTEGER NOT NULL, PRIMARY KEY(robot_id,group_id,message_id));
            CREATE TABLE IF NOT EXISTS command_reply_receipts (
                job_id TEXT NOT NULL, reply_key TEXT NOT NULL, resulting_message_ids TEXT NOT NULL,
                created_at INTEGER NOT NULL, PRIMARY KEY(job_id,reply_key));
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

    internal IngressEvent[] PendingIngress(DateTimeOffset? now = null)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT o.body FROM ingress_outbox o LEFT JOIN ingress_failures f USING(event_id)
                WHERE f.rejected_at IS NULL AND (f.next_attempt_at IS NULL OR f.next_attempt_at <= $now)
                ORDER BY o.created_at,o.rowid LIMIT 50
                """;
            cmd.Parameters.AddWithValue("$now", (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds());
            using var reader = cmd.ExecuteReader();
            var messages = new List<IngressEvent>();
            while (reader.Read()) messages.Add(JsonSerializer.Deserialize<IngressEvent>(reader.GetString(0), BridgeJson.Options)!);
            return messages.ToArray();
        }
    }

    internal bool IngressReady(string id)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM ingress_failures WHERE event_id=$id AND (rejected_at IS NOT NULL OR next_attempt_at>$now)";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            return cmd.ExecuteScalar() is null;
        }
    }

    internal void FailIngress(string id, int? status, bool permanent, int retrySeconds)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO ingress_failures VALUES($id,$status,1,$now+$delay,$rejected)
                ON CONFLICT(event_id) DO UPDATE SET status_code=$status, attempts=attempts+1,
                    next_attempt_at=$now+MIN(3600,$delay*(1 << MIN(attempts,10))), rejected_at=$rejected
                """;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$status", (object?)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.Parameters.AddWithValue("$delay", Math.Clamp(retrySeconds, 1, 3600));
            cmd.Parameters.AddWithValue("$rejected", permanent ? now : DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    internal void RemoveIngress(string id)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "DELETE FROM ingress_outbox WHERE event_id=$id; DELETE FROM ingress_failures WHERE event_id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
    }

    internal void SaveCommandOrigin(GroupMessageEventArgs e)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO command_origins VALUES($r,$g,$m,$s,$n,$role,$t)";
            cmd.Parameters.AddWithValue("$r", e.RobotId!);
            cmd.Parameters.AddWithValue("$g", e.GroupId);
            cmd.Parameters.AddWithValue("$m", e.MessageId);
            cmd.Parameters.AddWithValue("$s", e.SenderId);
            cmd.Parameters.AddWithValue("$n", e.SenderMemberCard);
            cmd.Parameters.AddWithValue("$role", (int)e.SenderMemberType);
            cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }
    }

    internal (string SenderId, string SenderName, MemberType Role)? CommandOrigin(ReplyDestination destination)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT sender_id,sender_name,member_type FROM command_origins WHERE robot_id=$r AND group_id=$g AND message_id=$m";
            cmd.Parameters.AddWithValue("$r", destination.RobotId);
            cmd.Parameters.AddWithValue("$g", destination.GroupId);
            cmd.Parameters.AddWithValue("$m", destination.ReplyToMessageId);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? (reader.GetString(0), reader.GetString(1), (MemberType)reader.GetInt32(2)) : null;
        }
    }

    internal string[]? CommandReply(string jobId, string key)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT resulting_message_ids FROM command_reply_receipts WHERE job_id=$j AND reply_key=$k";
            cmd.Parameters.AddWithValue("$j", jobId);
            cmd.Parameters.AddWithValue("$k", key);
            return cmd.ExecuteScalar() is string json ? JsonSerializer.Deserialize<string[]>(json) : null;
        }
    }

    internal void SaveCommandReply(CommandJob job, string key, string[] ids)
    {
        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            foreach (var id in ids)
                MapCore(job.Destination.RobotId, job.Destination.GroupId, id, job.ConversationId, tx);
            using var cmd = _db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR IGNORE INTO command_reply_receipts VALUES($j,$k,$ids,$t)";
            cmd.Parameters.AddWithValue("$j", job.JobId);
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$ids", JsonSerializer.Serialize(ids));
            cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
            tx.Commit();
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

    internal void CompleteCommand(CommandJob job)
    {
        lock (_gate)
        {
            var ids = new List<string>();
            using (var read = _db.CreateCommand())
            {
                read.CommandText = "SELECT resulting_message_ids FROM command_reply_receipts WHERE job_id=$j";
                read.Parameters.AddWithValue("$j", job.JobId);
                using var reader = read.ExecuteReader();
                while (reader.Read()) ids.AddRange(JsonSerializer.Deserialize<string[]>(reader.GetString(0))!);
            }
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "INSERT INTO queue_deliveries VALUES ($j,'sent',$c,$ids,$t) ON CONFLICT(job_id) DO NOTHING";
            cmd.Parameters.AddWithValue("$j", job.JobId);
            cmd.Parameters.AddWithValue("$c", job.ConversationId);
            cmd.Parameters.AddWithValue("$ids", JsonSerializer.Serialize(ids.Distinct()));
            cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }
    }

    internal void Prune(DateTimeOffset before)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                DELETE FROM message_conversations WHERE created_at<$t;
                DELETE FROM queue_deliveries WHERE status='sent' AND updated_at<$t;
                DELETE FROM command_origins WHERE created_at<$t;
                DELETE FROM command_reply_receipts WHERE created_at<$t;
                """;
            cmd.Parameters.AddWithValue("$t", before.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose() { lock (_gate) _db.Dispose(); }
}
