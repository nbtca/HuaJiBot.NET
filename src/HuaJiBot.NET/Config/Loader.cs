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
    internal static readonly object SaveLock = new();

    public string Save()
    {
        lock (SaveLock)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            // Write then rename, so a crash mid-write cannot leave a truncated config.
            const string tempFile = ConfigFileName + ".tmp";
            File.WriteAllText(tempFile, str);
            if (!OperatingSystem.IsWindows() && File.Exists(ConfigFileName))
                File.SetUnixFileMode(tempFile, File.GetUnixFileMode(ConfigFileName));
            File.Move(tempFile, ConfigFileName, true);
            return str;
        }
    }
}
