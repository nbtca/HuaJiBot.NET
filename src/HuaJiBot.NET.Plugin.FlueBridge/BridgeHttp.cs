using System.Net;

namespace HuaJiBot.NET.Plugin.FlueBridge;

internal static class BridgeHttp
{
    internal const string UserAgent = "HuaJiBot-Bridge/1.0";

    internal static HttpClientHandler CreateHandler(string? proxyValue)
    {
        Uri? proxy = null;
        if (!string.IsNullOrWhiteSpace(proxyValue)
            && (!Uri.TryCreate(proxyValue.Trim(), UriKind.Absolute, out proxy)
                || proxy.Scheme is not ("http" or "https")
                || string.IsNullOrWhiteSpace(proxy.Host) || proxy.Port <= 0
                || !string.IsNullOrEmpty(proxy.UserInfo) || proxy.AbsolutePath != "/"
                || !string.IsNullOrEmpty(proxy.Query) || !string.IsNullOrEmpty(proxy.Fragment)))
            throw new InvalidOperationException("HUAJIBOT_BRIDGE_PROXY must be an absolute HTTP or HTTPS proxy URL without credentials, path, query, or fragment.");

        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        if (proxy is not null) handler.Proxy = new WebProxy(proxy);
        return handler;
    }
}
