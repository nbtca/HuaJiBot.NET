using System;

namespace HuaJiBot.NET;

public abstract class EntryPointBase : Attribute { }

//[AttributeUsage(AttributeTargets.Module)]
//public class AdapterEntryPointAttribute<T>(string adapterName) : EntryPointBase { }

/// <summary>
/// 插件入口点标记
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="pluginName"></param>
[AttributeUsage(AttributeTargets.Module)]
public class PluginEntryPointAttribute<T>(string pluginName) : EntryPointBase { }
