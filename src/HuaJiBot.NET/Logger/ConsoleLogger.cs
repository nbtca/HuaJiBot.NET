﻿using System.Reflection;

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
}
