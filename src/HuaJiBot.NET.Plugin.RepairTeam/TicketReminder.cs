using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Utils;
using Timer = System.Timers.Timer;

namespace HuaJiBot.NET.Plugin.RepairTeam;

internal class TicketReminder : IDisposable
{
    private readonly IPluginService _service;
    private readonly PluginConfig _config;
    private readonly Func<DateTimeOffset, Task<List<Ticket>>> _fetch;
    private readonly string _statePath;
    private readonly Timer _timer = new(TimeSpan.FromHours(1));
    private DateOnly _lastSent;

    public TicketReminder(
        IPluginService service,
        PluginConfig config,
        Func<DateTimeOffset, Task<List<Ticket>>> fetch,
        string statePath
    )
    {
        _service = service;
        _config = config;
        _fetch = fetch;
        _statePath = statePath;
        if (File.Exists(statePath) && DateOnly.TryParse(File.ReadAllText(statePath), out var sent))
            _lastSent = sent;
        _timer.Elapsed += (_, _) => _ = CheckAsync(NetworkTime.Now);
    }

    public void Start()
    {
        _timer.Start();
        Task.Delay(30_000).ContinueWith(_ => CheckAsync(NetworkTime.Now));
    }

    public async Task CheckAsync(DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.Date);
        if (_config.RemindSince is null || now.Hour != _config.RemindHour || today == _lastSent)
            return;
        if (!await SendDigestAsync(now))
            return;
        _lastSent = today;
        try
        {
            File.WriteAllText(_statePath, today.ToString("O"));
        }
        catch (Exception ex)
        {
            _service.LogError("[RepairTeam] 保存工单提醒记录失败", ex);
        }
    }

    /// <returns>false when Saturday could not be read, so the check retries later.</returns>
    public async Task<bool> SendDigestAsync(DateTimeOffset now)
    {
        List<Ticket> tickets;
        try
        {
            tickets = await _fetch(_config.RemindSince!.Value);
        }
        catch (Exception ex)
        {
            _service.LogError("[RepairTeam] 读取维修工单失败", ex);
            return false;
        }
        var stalled = TicketDigest.Stalled(tickets, now, _config);
        if (stalled.Count == 0)
            return true;
        var post = TicketDigest.ToPost(
            stalled,
            now,
            TicketDigest.Format($"【维修工单提醒】{stalled.Count} 张工单卡住了", stalled, now)
        );
        foreach (var group in _config.RemindGroups)
            await _service.TrySendGroupMessageAsync(group, post);
        return true;
    }

    public void Dispose() => _timer.Dispose();
}
