using Microsoft.Extensions.Logging;

namespace HuaJiBot.NET.Logger;

public class PluginLoggerProvider(PluginBase plugin) : ILoggerProvider
{
    public void Dispose() { }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new PluginLogger(plugin, categoryName);
    }
}