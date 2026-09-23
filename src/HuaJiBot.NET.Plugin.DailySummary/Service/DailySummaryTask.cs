using System.Text.Json;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Plugin.DailySummary.Config;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

internal class DailySummaryTask : IDisposable
{
    private readonly IPluginService _service;
    private readonly PluginConfig _config;
    private readonly string _statePath;
    private readonly Func<string, DateTime, CancellationToken, Task> _summarize;
    private readonly CancellationTokenSource _cts = new();
    private readonly Timer _timer = new(TimeSpan.FromMinutes(1));
    private readonly Dictionary<string, DateOnly> _sent = [];
    private readonly Dictionary<string, int> _failures = [];
    private DateOnly _failuresDay;
    private int _running;

    public DailySummaryTask(
        IPluginService service,
        PluginConfig config,
        string statePath,
        Func<string, DateTime, CancellationToken, Task> summarize
    )
    {
        _service = service;
        _config = config;
        _statePath = statePath;
        _summarize = summarize;
        try
        {
            if (File.Exists(statePath))
                _sent = JsonSerializer.Deserialize<Dictionary<string, DateOnly>>(
                    File.ReadAllText(statePath)
                )!;
        }
        catch (Exception ex)
        {
            _service.LogError("[每日总结] 读取发送记录失败", ex);
        }
        _timer.Elapsed += (_, _) => _ = RunIfDueAsync(Utils.NetworkTime.Now);
    }

    public void Start()
    {
        _timer.Start();
        _service.Log($"[每日总结] 定时任务已启动，将在每天 {_config.SummaryHour:D2}:{_config.SummaryMinute:D2} 执行");
    }

    public async Task RunIfDueAsync(DateTimeOffset now)
    {
        if (now.Hour != _config.SummaryHour || now.Minute < _config.SummaryMinute)
            return;
        if (Interlocked.Exchange(ref _running, 1) == 1)
            return;
        try
        {
            var today = DateOnly.FromDateTime(now.Date);
            if (_failuresDay != today)
            {
                _failures.Clear();
                _failuresDay = today;
            }
            foreach (var groupId in _config.GroupIds)
            {
                if (
                    _sent.GetValueOrDefault(groupId) == today
                    || _failures.GetValueOrDefault(groupId) >= _config.MaxRetryCount
                )
                    continue;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(_config.LlmTimeoutSeconds));
                try
                {
                    await _summarize(groupId, now.Date.AddDays(-1), cts.Token);
                    _sent[groupId] = today;
                    Save();
                }
                catch (Exception ex)
                {
                    var attempts = _failures[groupId] = _failures.GetValueOrDefault(groupId) + 1;
                    _service.LogError($"[每日总结] 群组 {groupId} 总结失败（第 {attempts} 次）", ex);
                }
            }
        }
        finally
        {
            _running = 0;
        }
    }

    private void Save()
    {
        try
        {
            var tmp = _statePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_sent));
            File.Move(tmp, _statePath, true);
        }
        catch (Exception ex)
        {
            _service.LogError("[每日总结] 保存发送记录失败", ex);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer.Dispose();
        _cts.Dispose();
    }
}
