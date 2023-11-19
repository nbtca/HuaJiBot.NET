using System;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET;

public abstract class EntryPointBase : Attribute
{
    /// <summary>
    /// 插件名称
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 插件描述
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 插件实例/对象
    /// </summary>
    public abstract PluginBase CreateInstance(BotServiceBase api);
}

//[AttributeUsage(AttributeTargets.Module)]
//public class AdapterEntryPointAttribute<T>(string adapterName) : EntryPointBase { }

/// <summary>
/// 插件入口点标记
/// </summary>
/// <typeparam name="T">入口点类，必须继承自<see cref="PluginBase"/></typeparam>
/// <param name="pluginName">插件名</param>
/// <param name="description">插件描述</param>
[AttributeUsage(AttributeTargets.Assembly)]
public class PluginEntryPointAttribute<T>(string pluginName, string description) : EntryPointBase
    where T : PluginBase, new()
{
    public override string Name { get; } = pluginName;
    public override string Description { get; } = description;

    public override PluginBase CreateInstance(BotServiceBase api) =>
        new T { Name = pluginName, Service = api };
}
