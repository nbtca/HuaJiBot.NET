using System.Net;
using HuaJiBot.NET.Plugin.FlueBridge;

namespace HuaJiBot.NET.UnitTest;

public partial class FlueBridgeTest
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UnconfiguredBridgeProxyKeepsDefaultNetworkingAndCertificateValidation(string? value)
    {
        using var handler = BridgeHttp.CreateHandler(value);
        Assert.Multiple(() =>
        {
            Assert.That(handler.Proxy, Is.Null);
            Assert.That(handler.UseProxy, Is.True);
            Assert.That(handler.AllowAutoRedirect, Is.False);
            Assert.That(handler.ServerCertificateCustomValidationCallback, Is.Null);
        });
    }

    [TestCase("http://proxy.example:7890")]
    [TestCase("https://proxy.example:8443/")]
    public void ConfiguredProxyAppliesOnlyToItsBridgeHandler(string value)
    {
        using var configured = BridgeHttp.CreateHandler(value);
        using var unconfigured = BridgeHttp.CreateHandler(null);
        Assert.Multiple(() =>
        {
            Assert.That(configured.Proxy, Is.TypeOf<WebProxy>());
            Assert.That(configured.Proxy!.GetProxy(new Uri("https://agent.example/health")), Is.EqualTo(new Uri(value)));
            Assert.That(configured.UseProxy, Is.True);
            Assert.That(configured.AllowAutoRedirect, Is.False);
            Assert.That(configured.ServerCertificateCustomValidationCallback, Is.Null);
            Assert.That(unconfigured.Proxy, Is.Null);
        });
    }

    [TestCase("proxy.example:7890")]
    [TestCase("file:///private-proxy")]
    [TestCase("ftp://proxy.example")]
    [TestCase("http://proxy.example:0")]
    [TestCase("http://proxy.example:99999")]
    [TestCase("http://user:private-password@proxy.example:7890")]
    [TestCase("http://proxy.example:7890/private-path")]
    [TestCase("http://proxy.example:7890/?private-query")]
    [TestCase("http://proxy.example:7890/#private-fragment")]
    public void InvalidProxyFailsWithoutDisclosingItsValue(string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => BridgeHttp.CreateHandler(value));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.StartWith("HUAJIBOT_BRIDGE_PROXY must be"));
            Assert.That(error.ToString(), Does.Not.Contain(value));
            Assert.That(error.InnerException, Is.Null);
        });
    }
}
