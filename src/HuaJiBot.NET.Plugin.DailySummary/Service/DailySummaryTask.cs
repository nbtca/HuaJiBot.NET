using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Plugin.DailySummary.Config;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.DailySummary.Service;

internal class DailySummaryTask : IDisposable
{
    private readonly IPluginService _service;
    private readonly PluginConfig _config;
    private readonly Func<string, Task> _generateSummary;
    private readonly Timer _timer;
    private DateTime _lastRunDate = DateTime.MinValue;

    public DailySummaryTask(
        IPluginService service,
        PluginConfig config,
        Func<string, Task> generateSummary
    )
    {
        _service = service;
        _config = config;
        _generateSummary = generateSummary;

        // 每分钟检查一次
        _timer = new Timer(TimeSpan.FromMinutes(1));
        _timer.Elapsed += (_, _) => _ = CheckAndRunSummaryAsync();
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
        _service.Log($"[每日总结] 定时任务已启动，将在每天 {_config.SummaryHour:D2}:{_config.SummaryMinute:D2} 执行");
    }

    private async Task CheckAndRunSummaryAsync()
    {
        try
        {
            var now = Utils.NetworkTime.Now;

            // 检查是否到了执行时间
            if (now.Hour != _config.SummaryHour || now.Minute != _config.SummaryMinute)
                return;

            // 检查今天是否已经执行过
            if (_lastRunDate.Date == now.Date)
                return;

            _lastRunDate = now.Date;
            _service.Log("[每日总结] 开始执行每日总结任务");

            foreach (var groupId in _config.GroupIds)
            {
                try
                {
                    await _generateSummary(groupId);
                }
                catch (Exception ex)
                {
                    _service.LogError($"[每日总结] 群组 {groupId} 总结失败", ex.Message);
                }
            }

            _service.Log("[每日总结] 每日总结任务完成");
        }
        catch (Exception ex)
        {
            _service.LogError("[每日总结] 定时任务异常", ex.Message);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
