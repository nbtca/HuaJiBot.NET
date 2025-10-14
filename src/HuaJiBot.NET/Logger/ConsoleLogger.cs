using System.Reflection;
using Microsoft.Extensions.Logging;

namespace HuaJiBot.NET.Logger;

public class ConsoleLogger : ILogger
{
    public void Log(object message)
    {
        Console.WriteLine($"[{Utils.NetworkTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}");
    }

    public void Warn(object message)
    {
        Console.WriteLine($"[{Utils.NetworkTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}");
    }

    public void LogDebug(object message)
    {
        Console.WriteLine($"[{Utils.NetworkTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] {message}");
    }

    public void LogError(object message, object? detail)
    {
        var detailStr = detail switch
        {
            TargetInvocationException e => e.ToString(),
            Exception e => e.ToString(),
            _ => detail?.ToString(),
        };
        Console.WriteLine(
            $"[{Utils.NetworkTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}"
                + $"{Environment.NewLine}---{Environment.NewLine}"
                + $"{detailStr}"
                + $"{Environment.NewLine}---"
        );
    }

    // Implementation of Microsoft.Extensions.Logging.ILogger interface
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = Utils.NetworkTime.Now;
        var logLevelStr = logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            LogLevel.None => "NONE",
            _ => logLevel.ToString().ToUpper()
        };

        if (exception != null)
        {
            Console.WriteLine(
                $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{logLevelStr}] {message}"
                    + $"{Environment.NewLine}---{Environment.NewLine}"
                    + $"{exception}"
                    + $"{Environment.NewLine}---"
            );
        }
        else
        {
            Console.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{logLevelStr}] {message}");
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Enable all log levels by default
        return logLevel != LogLevel.None;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        // Console logger doesn't support scopes by default
        // Return null to indicate no scope is created
        return null;
    }
}
