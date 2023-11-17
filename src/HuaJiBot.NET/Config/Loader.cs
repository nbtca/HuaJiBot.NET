using System.IO;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Config;

public partial class Config
{
    /// <summary>
    /// 配置文件名
    /// </summary>
    private const string ConfigFileName = "config.json";

    /// <summary>
    /// 加载配置文件并反序列化
    /// </summary>
    /// <returns>配置对象</returns>
    public static Config Load()
    {
        if (!File.Exists(ConfigFileName))
        {
            return new Config();
        }
        return JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFileName))
            ?? new Config();
    }

    /// <summary>
    /// 序列号json并保存配置文件
    /// </summary>
    public void Save()
    {
        File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
