using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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

public abstract class PluginBase
{
    public string Name { get; internal set; } = null!;
    public BotServiceBase Service { get; internal set; } = null!;

    public void Info(object msg) => Service.Log($"[{Name}] {msg}");

    public void Error(object msg, object detail) => Service.LogError($"[{Name}] {msg}", detail);

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

    public IEnumerable<CommandInfo> GetAllCommands()
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
                        method.Invoke(this, args);
                    },
                    arguments
                );
            }
        }
    }
}
