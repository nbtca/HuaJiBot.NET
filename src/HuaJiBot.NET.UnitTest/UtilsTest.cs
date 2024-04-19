namespace HuaJiBot.NET.UnitTest;

internal class UtilsTest
{
    [SetUp]
    public void Setup() { }

    [Test]
    public async Task Test1()
    {
        var time = await Utils.NetworkTime.GetNetworkTime("ntp1.aliyun.com");
        //var time = await Utils.NetworkTime.GetNetworkTime();
        Console.WriteLine(time);
    }
}
