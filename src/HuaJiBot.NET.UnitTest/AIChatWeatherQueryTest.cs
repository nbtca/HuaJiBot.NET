using HuaJiBot.NET.Plugin.AIChat;

namespace HuaJiBot.NET.UnitTest;

internal class AIChatWeatherQueryTest
{
    [TestCase("今天宁波天气如何", "宁波")]
    [TestCase("宁波今天的天气", "宁波")]
    [TestCase("今天天气如何", "宁波")]
    public void CurrentWeatherQueriesResolveCity(string query, string expected)
    {
        Assert.That(PluginMain.CurrentWeatherCity(query, "宁波"), Is.EqualTo(expected));
    }

    [Test]
    public void FutureWeatherIsNotSentToCurrentWeatherTool()
    {
        Assert.That(PluginMain.CurrentWeatherCity("明天宁波天气如何", "宁波"), Is.Null);
    }
}
