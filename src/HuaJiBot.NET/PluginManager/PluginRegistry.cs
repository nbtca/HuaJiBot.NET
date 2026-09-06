using System.Collections.Concurrent;

namespace HuaJiBot.NET.PluginManager;

public class PluginRegistry
{
    private readonly ConcurrentDictionary<string, (EntryPointBase entryPoint, PluginBase instance)> _plugins = new();

    public void Add(EntryPointBase entryPoint, PluginBase instance)
    {
        if (!_plugins.TryAdd(entryPoint.Name, (entryPoint, instance)))
            throw new InvalidOperationException($"Duplicate plugin name: '{entryPoint.Name}'");
    }

    public IReadOnlyCollection<(EntryPointBase entryPoint, PluginBase instance)> GetAll()
        => _plugins.Values.ToList().AsReadOnly();

    public void Clear() => _plugins.Clear();
}
