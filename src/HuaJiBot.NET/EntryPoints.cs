﻿using System;

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
    protected abstract Lazy<PluginBase> InstanceLazy { get; }
    public PluginBase Instance => InstanceLazy.Value;
}

//[AttributeUsage(AttributeTargets.Module)]
//public class AdapterEntryPointAttribute<T>(string adapterName) : EntryPointBase { }

/// <summary>
/// 插件入口点标记
/// </summary>
/// <typeparam name="T">入口点类，必须继承自<see cref="PluginBase"/></typeparam>
/// <param name="pluginName">插件名</param>
/// <param name="description">插件描述</param>
[AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
public class PluginEntryPointAttribute<T>(string pluginName, string description) : EntryPointBase
    where T : PluginBase, new()
{
    public override string Name { get; } = pluginName;
    public override string Description { get; } = description;
    protected override Lazy<PluginBase> InstanceLazy { get; } = new(() => new T());
}