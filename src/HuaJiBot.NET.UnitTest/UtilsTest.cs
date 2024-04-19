using Newtonsoft.Json;

namespace HuaJiBot.NET.UnitTest;

internal class UtilsTest
{
    [SetUp]
    public Task SetupAsync()
    {
        return Utils.NetworkTime.UpdateDiffAsync();
    }

    [Test]
    public void Test1()
    {
        var time = Utils.NetworkTime.Now;
        Console.WriteLine(time.ToString("s"));
        Console.WriteLine(time);
        Console.WriteLine(Utils.NetworkTime.Diff);
    }
}
