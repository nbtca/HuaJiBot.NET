using System.Text;
using Microsoft.Extensions.Logging;

namespace HuaJiBot.NET.Logger;

public class PluginLogger(PluginBase plugin, string categoryName)
    : Microsoft.Extensions.Logging.ILogger
{
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        var fullText = new StringBuilder();
        fullText.Append($"[{categoryName}] {formatter(state, exception)}");
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
                plugin.Info(fullText);
                break;
            case LogLevel.Warning:
                plugin.Warn(fullText, exception);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
            case LogLevel.None:
                plugin.Error(fullText, exception);
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }
}