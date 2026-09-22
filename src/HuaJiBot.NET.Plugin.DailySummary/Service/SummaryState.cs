using System.Text.Json;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

/// <summary>
/// 当天各群的定时总结进度。持久化使得在总结时间窗内重启既不重发也不漏发；
/// 新的一天第一次检查时丢弃前一天所有记录。
/// </summary>
internal class SummaryState
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public sealed class GroupRecord
    {
        public bool Sent { get; set; }
        public int Attempts { get; set; }
    }

    public sealed class StateFile
    {
        public DateOnly Date { get; set; }
        public Dictionary<string, GroupRecord> Groups { get; set; } = new();
    }

    private readonly string _path;
    private StateFile _state = new();

    public SummaryState(string path)
    {
        _path = path;
        if (!File.Exists(path))
            return;
        try
        {
            _state =
                JsonSerializer.Deserialize<StateFile>(File.ReadAllText(path)) ?? new StateFile();
        }
        catch (JsonException)
        {
            _state = new StateFile();
        }
    }

    /// <summary>丢弃属于前一天的全部记录。</summary>
    public void RollOver(DateOnly today)
    {
        if (_state.Date == today)
            return;
        _state = new StateFile { Date = today };
        Save();
    }

    public GroupRecord Get(string groupId) =>
        _state.Groups.TryGetValue(groupId, out var record) ? record : new GroupRecord();

    public bool IsDue(string groupId, int maxRetryCount)
    {
        var record = Get(groupId);
        return !record.Sent && record.Attempts < maxRetryCount;
    }

    public void MarkSent(string groupId) => Upsert(groupId, sent: true);

    public void MarkFailure(string groupId) => Upsert(groupId, sent: false);

    private void Upsert(string groupId, bool sent)
    {
        if (!_state.Groups.TryGetValue(groupId, out var record))
        {
            record = new GroupRecord();
            _state.Groups[groupId] = record;
        }
        record.Sent = sent;
        if (!sent)
            record.Attempts++;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(dir);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_state, JsonOptions));
        File.Move(tmp, _path, overwrite: true);
    }
}
