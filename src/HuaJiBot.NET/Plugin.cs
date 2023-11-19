using System.Threading.Tasks;
using HuaJiBot.NET.Bot;
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
    /// 插件初始化
    /// </summary>
    protected internal abstract Task Initialize();

    /// <summary>
    /// 插件卸载
    /// </summary>
    protected internal abstract void Unload();
}
