using HuaJiBot.NET.Bot;
using Microsoft.Extensions.Logging;

namespace HuaJiBot.NET.Logger;

public class PluginLoggerProvider(BotService service) : ILoggerProvider
{
    public void Dispose() { }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new PluginLogger(service, categoryName);
    }
}
