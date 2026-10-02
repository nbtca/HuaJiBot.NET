using System.Threading.Channels;
using HuaJiBot.NET.Events;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Plugin.FlueBridge;

public sealed class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    public PluginConfig Config { get; } = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly Channel<GroupMessageEventArgs> _incoming = Channel.CreateBounded<GroupMessageEventArgs>(100);
    private BridgeStore? _store;
    private HttpClient? _http;
    private BridgeEngine? _engine;
    private IngressClient? _ingress;
    private IReplyQueue? _queue;
    private Task? _running;
    private long _lastActiveTicks;

    protected override Task InitializeAsync()
    {
        Config.Validate();
        var hmac = Environment.GetEnvironmentVariable("HUAJIBOT_BRIDGE_HMAC_SECRET");
        var token = Environment.GetEnvironmentVariable("HUAJIBOT_QUEUE_API_TOKEN");
        if (string.IsNullOrWhiteSpace(hmac) || string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Set HUAJIBOT_BRIDGE_HMAC_SECRET and HUAJIBOT_QUEUE_API_TOKEN in the environment.");
        if (Service.Config.TryGetLive("AIChat", out var ai) && ai.Enabled)
        {
            var groups = JObject.FromObject(ai)["GroupIds"]?.Values<string>().ToArray() ?? [];
            if (Config.Destinations.Any(d => groups.Contains(d.GroupId)))
                throw new InvalidOperationException("AIChat and FlueBridge groups must be mutually exclusive.");
        }
        _store = new BridgeStore(Path.Combine(Service.GetPluginDataPath(), "flue_bridge.db"));
        // Signed requests and Queue credentials must never follow a redirect to another host.
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30) };
        _ingress = new IngressClient(_http, new Uri(Config.IngressUrl), hmac, Config.MaxMessageBytes);
        _queue = new CloudflareQueue(_http, Config, token);
        _engine = new BridgeEngine(Config, _store, Service, _queue, message => Warn(message));
        Service.Events.OnGroupMessageReceived += Receive;
        _running = Task.WhenAll(ForwardLoopAsync(_stop.Token), PullLoopAsync(_stop.Token));
        Info("Flue桥接已启动");
        return Task.CompletedTask;
    }

    private void Receive(object? sender, GroupMessageEventArgs e)
    {
        if (e.RobotId is { } robot && Config.Allows(robot, e.GroupId) && !_incoming.Writer.TryWrite(e))
            Warn("Flue ingress buffer is full; message was not forwarded.");
    }

    private void Activate()
    {
        Interlocked.Exchange(ref _lastActiveTicks, DateTimeOffset.UtcNow.UtcTicks);
        try { _wake.Release(); } catch (SemaphoreFullException) { }
    }

    private async Task ForwardLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var e in _incoming.Reader.ReadAllAsync(ct))
            {
                try
                {
                    var message = _engine!.Normalize(e);
                    if (message is null) continue;
                    for (var attempt = 0; ; attempt++)
                    {
                        try
                        {
                            await _ingress!.SendAsync(message, ct);
                            Activate();
                            break;
                        }
                        catch (HttpRequestException ex) when (attempt < 2 && (ex.StatusCode is null
                            || (int)ex.StatusCode >= 500 || (int)ex.StatusCode == 429))
                        { await Task.Delay(TimeSpan.FromSeconds(Config.RetryDelaySeconds), ct); }
                        catch (OperationCanceledException) when (!ct.IsCancellationRequested && attempt < 2)
                        { await Task.Delay(TimeSpan.FromSeconds(Config.RetryDelaySeconds), ct); }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception) { Warn("Agent ingress forwarding failed."); }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task PullLoopAsync(CancellationToken ct)
    {
        var nextPrune = DateTimeOffset.MinValue;
        var recovering = false;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (DateTimeOffset.UtcNow >= nextPrune)
                    {
                        _store!.Prune(DateTimeOffset.UtcNow.AddDays(-Config.RetentionDays));
                        nextPrune = DateTimeOffset.UtcNow.AddHours(1);
                    }
                    var leases = await _queue!.PullAsync(ct);
                    if (recovering || leases.Count > 0) Activate();
                    recovering = false;
                    foreach (var lease in leases) await _engine!.DeliverAsync(lease, ct);
                    if (leases.Count > 0) continue;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception)
                {
                    recovering = true;
                    Warn("Queue polling failed; leases remain available for redelivery.");
                    await Task.Delay(TimeSpan.FromSeconds(Config.RetryDelaySeconds), ct);
                }
                var age = (DateTimeOffset.UtcNow.UtcTicks - Interlocked.Read(ref _lastActiveTicks)) / (double)TimeSpan.TicksPerSecond;
                var delay = age < Config.ActiveWindowSeconds ? 1
                    : Math.Min(Config.IdlePollSeconds, 1 + (int)Math.Min(Config.IdlePollSeconds, (age - Config.ActiveWindowSeconds) / 10));
                await _wake.WaitAsync(TimeSpan.FromSeconds(delay), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    protected override void Unload()
    {
        Service.Events.OnGroupMessageReceived -= Receive;
        _incoming.Writer.TryComplete();
        _stop.Cancel();
        // Defer disposal until any adapter send has returned and persisted its receipt.
        if (_running is { } running) _ = running.ContinueWith(_ => Cleanup(), TaskScheduler.Default);
        else Cleanup();
    }

    private void Cleanup()
    {
        _http?.Dispose();
        _store?.Dispose();
        _wake.Dispose();
        _stop.Dispose();
    }
}
