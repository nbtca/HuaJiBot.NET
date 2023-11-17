using System.Threading.Tasks;
using HuaJiBot.NET.Bot;
using Newtonsoft.Json;

namespace HuaJiBot.NET;

public class ConfigBase
{
    [JsonProperty(Order = -2)]
    public bool Enabled { get; set; } = true;
}
public interface IPluginWithConfig<out T> where T : ConfigBase, new()
{
    T Config { get; }
}
public abstract class PluginBase
{
    public BotServiceBase Service => Global.Service;
    public string Name { get; internal set; } = null!;

    /// <summary>
    /// 插件初始化
    /// </summary>
    protected internal abstract Task Initialize();

    /// <summary>
    /// 插件卸载
    /// </summary>
    protected internal abstract void Unload();
}
