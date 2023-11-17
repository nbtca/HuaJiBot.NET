using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Config;

public class ConfigWrapper(Config config)
{
    private readonly Dictionary<string, ConfigBase> _plugins = new();
    public void Populate(string key, ConfigBase existValue) //填充配置
    {
        if (config.Plugins.TryGetValue(key, out var value)) //如果配置中有这个插件的配置
        {
            using var jsonReader = value.CreateReader(); //创建读取器
            JsonSerializer.CreateDefault().Populate(jsonReader, existValue); //从读取器填充json配置到对象(反序列化)
        }
        _plugins[key] = existValue; //添加到插件列表
    }
    public void Save()
    {
        foreach (var (key, value) in _plugins)
        {
            config.Plugins[key] = JObject.FromObject(value);//将插件配置转换为JObject(序列化)
        }
        config.Save();
    }
}