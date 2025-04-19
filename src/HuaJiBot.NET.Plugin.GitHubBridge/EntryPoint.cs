using PluginMain = HuaJiBot.NET.Plugin.GitHubBridge.PluginMain;

//标注插件入口点
[assembly: HuaJiBot.NET.PluginEntryPoint<PluginMain>("GitHubBridge", "到GitHub的消息桥接")]
