using System.Text;
using HuaJiBot.NET.Bot;
using Microsoft.Extensions.Logging;

namespace HuaJiBot.NET.Logger;

public class PluginLogger(BotService service, string categoryName)
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
                service.Log(fullText);
                break;
            case LogLevel.Warning:
                service.Warn($"{fullText}\n{exception}");
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
            case LogLevel.None:
                service.LogError(fullText, exception);
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
