namespace HuaJiBot.NET.Plugin.AIChat.Config;

public class PluginConfig : ConfigBase
{
    public string SystemPrompt = "你是一个有用的AI助手";
    public ModelConfig Model = new();
}
