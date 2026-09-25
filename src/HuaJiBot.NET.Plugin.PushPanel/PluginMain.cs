using System.Net;
using HuaJiBot.NET.Config;

namespace HuaJiBot.NET.Plugin.PushPanel;

public class PluginConfig : ConfigBase
{
    /// <summary>面板口令。为空时不监听。</summary>
    public string Token { get; set; } = "";

    /// <summary>监听地址。默认只绑本机。</summary>
    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 8787;

    /// <summary>允许 Host 不是本机地址。口令仍然必须带上。</summary>
    public bool AllowRemote { get; set; }
}

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    public PluginConfig Config { get; } = new();
    private PanelServer? _server;

    protected override void Initialize()
    {
        if (string.IsNullOrWhiteSpace(Config.Token))
        {
            Service.Log("[PushPanel] 未配置 Token，面板不监听");
            return;
        }
        if (Config.Port is < 1 or > 65535)
        {
            Service.Warn("[PushPanel] Port 不在 1–65535，面板不监听");
            return;
        }
        if (!Config.AllowRemote && !IsLoopback(Config.Host))
        {
            Service.Warn("[PushPanel] Host 不是本机地址。要对外监听请把 AllowRemote 设为 true");
            return;
        }
        try
        {
            _server = new PanelServer(Config.Host, Config.Port, Config.Token, Service.Config);
            _server.Start();
            Service.Log($"[PushPanel] http://{Config.Host}:{Config.Port}/");
        }
        catch (Exception ex)
        {
            Service.LogError("[PushPanel] 启动失败", ex);
            _server = null;
        }
    }

    protected override void Unload()
    {
        _server?.Dispose();
        _server = null;
    }

    internal static bool IsLoopback(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host == "127.0.0.1"
        || host == "::1"
        || (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
}
