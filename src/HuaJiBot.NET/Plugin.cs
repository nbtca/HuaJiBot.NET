using System.Reflection;
using System.Runtime.CompilerServices;
using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using Newtonsoft.Json;

namespace HuaJiBot.NET;

public class ConfigBase
{
    [JsonProperty(Order = -2)]
    public bool Enabled { get; set; } = true;
}

public interface IPluginWithConfig<out T>
    where T : ConfigBase, new()
{
    T Config { get; }
}

public abstract partial class PluginBase
{
    public bool Enabled { get; internal set; } = true;
    public string Name { get; internal set; } = null!;
    public BotService Service { get; internal set; } = null!;

    public void Info(
        object msg,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Service.Log($"[{Name}] {msg}", file, line);

    public void Warn(
        object msg,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Service.Warn($"[{Name}] {msg}", file, line);

    public void Warn(
        object msg,
        object? detail,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Service.Warn($"[{Name}] {msg}" + detail, file, line);

    public void Error(
        object msg,
        object? detail,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int? line = null
    ) => Service.LogError($"[{Name}] {msg}", detail, file, line);

    /// <summary>
    /// 插件初始化(异步)
    /// </summary>
    protected internal virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// 插件初始化
    /// </summary>
    protected internal virtual void Initialize() { }

    /// <summary>
    /// 插件卸载
    /// </summary>
    protected internal abstract void Unload();

    public record CommandInfo(
        string Name,
        string Description,
        Action<object?[]?> Method,
        CommandArgumentInfo[] Arguments
    );

    public record CommandArgumentInfo(
        CommandArgumentAttribute Attribute,
        Type Type,
        bool IsOptional,
        object? DefaultValue
    );

    public virtual IEnumerable<CommandInfo> GetAllCommands()
    {
        var methods = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<CommandAttribute>() is { } overload)
            {
                var arguments = (
                    from x in method.GetParameters()
                    select new CommandArgumentInfo(
                        x.GetCustomAttribute<CommandArgumentAttribute>()
                            ?? new CommandArgumentUnknownAttribute(x.ParameterType),
                        x.ParameterType,
                        x.IsOptional,
                        x.HasDefaultValue ? x.DefaultValue : null
                    )
                ).ToArray();
                yield return new CommandInfo(
                    overload.Key,
                    overload.Description,
                    args =>
                    {
                        var result = method.Invoke(this, args);
                        if (result is Task task)
                        {
                            task.ContinueWith(
                                t =>
                                {
                                    if (t.Exception is not null)
                                    {
                                        Error($"执行命令 {overload.Key} 时出现异常：", t.Exception);
                                    }
                                },
                                TaskContinuationOptions.OnlyOnFaulted
                            );
                        }
                    },
                    arguments
                );
            }
        }
    }

    public virtual IEnumerable<AgentFunctionInfo>? ExportFunctions => null;
}
