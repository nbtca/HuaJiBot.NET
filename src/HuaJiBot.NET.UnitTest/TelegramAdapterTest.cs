using HuaJiBot.NET.Adapter.Telegram;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.UnitTest;

public class TelegramAdapterTest
{
    [Test]
    public void TelegramAdapter_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var botToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
        
        // Act
        var adapter = new TelegramAdapter(botToken)
        {
            Logger = new ConsoleLogger()
        };
        
        // Assert
        Assert.That(adapter, Is.Not.Null);
        Assert.That(adapter.Logger, Is.Not.Null);
        Assert.That(adapter.AllRobots, Is.Empty); // Should be empty before login
    }
    
    [Test]
    public void TelegramAdapter_GetPluginDataPath_ShouldReturnValidPath()
    {
        // Arrange
        var botToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
        var adapter = new TelegramAdapter(botToken)
        {
            Logger = new ConsoleLogger()
        };
        
        // Act
        var path = adapter.GetPluginDataPath();
        
        // Assert
        Assert.That(path, Is.Not.Null);
        Assert.That(path, Does.Contain("plugins"));
        Assert.That(path, Does.Contain("data"));
        Assert.That(Directory.Exists(path), Is.True);
    }
    
    [Test]
    public void TelegramAdapter_GetNick_ShouldReturnUserId()
    {
        // Arrange
        var botToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11";
        var adapter = new TelegramAdapter(botToken)
        {
            Logger = new ConsoleLogger()
        };
        var userId = "123456789";
        
        // Act
        var nick = adapter.GetNick("botId", userId);
        
        // Assert
        Assert.That(nick, Is.EqualTo(userId));
    }
}