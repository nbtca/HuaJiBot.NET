using System;

namespace HuaJiBot.NET;

public abstract class EntryPointBase : Attribute { }

[AttributeUsage(AttributeTargets.Module)]
public class AdapterEntryPointAttribute<T>(string adapterName) : EntryPointBase { }

[AttributeUsage(AttributeTargets.Module)]
public class PluginEntryPointAttribute<T>(string pluginName) : EntryPointBase { }
