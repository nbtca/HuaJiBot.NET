using HuaJiBot.NET.Adapter.Kook;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.UnitTest;

internal class KookAdapterTest
{
    private static KookAdapter Adapter() =>
        new("1/MTA=/token") { Logger = new ConsoleLogger() };

    [Test]
    public void Constructor_AllRobotsEmptyBeforeLogin() =>
        Assert.That(Adapter().AllRobots, Is.Empty);

    [Test]
    public void SetGroupName_OnDirectChannel_ThrowsNotSupported()
    {
        IMessageService svc = Adapter();
        var chatCode = Guid.NewGuid().ToString();
        Assert.Throws<NotSupportedException>(() => svc.SetGroupName(null, chatCode, "name"));
    }

    [Test]
    public void SetGroupName_OnInvalidIdentifier_ThrowsArgument()
    {
        IMessageService svc = Adapter();
        Assert.Throws<ArgumentException>(() => svc.SetGroupName(null, "not-an-id", "name"));
    }

    [Test]
    public void GetMemberType_OnDirectChannel_ReturnsUnknown()
    {
        IAdapterService svc = Adapter();
        var chatCode = Guid.NewGuid().ToString();
        Assert.That(svc.GetMemberType("bot", chatCode, "123"), Is.EqualTo(MemberType.Unknown));
    }

    [Test]
    public void GetMemberType_OnInvalidIdentifier_ReturnsUnknown()
    {
        IAdapterService svc = Adapter();
        Assert.That(svc.GetMemberType("bot", "not-an-id", "123"), Is.EqualTo(MemberType.Unknown));
    }

    [Test]
    public void GetNick_OnNonNumericId_ReturnsIdVerbatim()
    {
        IAdapterService svc = Adapter();
        Assert.That(svc.GetNick("bot", "abc"), Is.EqualTo("abc"));
    }
}
