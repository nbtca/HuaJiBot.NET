using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Plugin.DailySummary.Config;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

internal class DailySummaryTask : IDisposable
{
    private readonly IPluginService _service;
    private readonly PluginConfig _config;
    private readonly SummaryState _state;
    private readonly Func<string, DateTime, CancellationToken, Task<bool>> _generate;
    private readonly CancellationTokenSource _cts = new();
    private readonly Timer _timer;
    private int _running;

    /// <param name="generate">执行单个群的总结并发送；
    /// 返回 false 表示消息不足被跳过，抛异常表示失败。</param>
    public DailySummaryTask(
        IPluginService service,
        PluginConfig config,
        SummaryState state,
        Func<string, DateTime, CancellationToken, Task<bool>> generate
    )
    {
        _service = service;
        _config = config;
        _state = state;
        _generate = generate;

        // 每分钟检查一次是否到达执行窗口（SummaryHour 整小时内可重试）
        _timer = new Timer(TimeSpan.FromMinutes(1));
        _timer.Elapsed += (_, _) => _ = RunIfDueAsync(Utils.NetworkTime.Now);
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
        _service.Log(
            $"[每日总结] 定时任务已启动，每天 {_config.SummaryHour:D2}:{_config.SummaryMinute:D2}（北京时间）总结 {Utils.NetworkTime.Now.Date.AddDays(-_config.SummaryDaysAgo):yyyy-MM-dd} 的消息，失败在该小时内最多重试 {_config.MaxRetryCount} 次"
        );
    }

    public async Task RunIfDueAsync(DateTimeOffset now)
    {
        try
        {
            // 新的一天开始时丢弃旧记录（含已成功的），状态文件只保留当天条目。
            _state.RollOver(DateOnly.FromDateTime(now.Date));

            if (now.Hour != _config.SummaryHour || now.Minute < _config.SummaryMinute)
                return;

            // 上一轮还没跑完（例如都在超时等待中）时不叠加执行。
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return;
            try
            {
                await RunDueGroupsAsync(now.Date, _cts.Token);
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }
        catch (Exception ex)
        {
            _service.LogError("[每日总结] 定时任务异常", ex);
        }
    }

    private async Task RunDueGroupsAsync(DateTime runDate, CancellationToken ct)
    {
        var targetDate = runDate.AddDays(-Math.Max(0, _config.SummaryDaysAgo));
        var due = _config.GroupIds.Where(g => _state.IsDue(g, _config.MaxRetryCount)).ToArray();
        if (due.Length == 0)
            return;

        var semaphore = new SemaphoreSlim(Math.Max(1, _config.MaxConcurrentGroups));
        await Task.WhenAll(
            due.Select(async groupId =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    using var groupCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    groupCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _config.LlmTimeoutSeconds)));
                    try
                    {
                        var sent = await _generate(groupId, targetDate, groupCts.Token);
                        // 消息不足同样记为完成：当天不再重试刷屏日志。
                        _state.MarkSent(groupId);
                        _service.Log(
                            $"[每日总结] 群组 {groupId} {(sent ? "总结完成" : "消息不足，跳过")}"
                        );
                    }
                    catch (Exception ex)
                    {
                        _state.MarkFailure(groupId);
                        _service.LogError(
                            $"[每日总结] 群组 {groupId} 失败（第 {_state.Get(groupId).Attempts} 次尝试）",
                            ex
                        );
                    }
                    _state.Save();
                }
                finally
                {
                    semaphore.Release();
                }
            })
        );
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer.Dispose();
        _cts.Dispose();
    }
}
