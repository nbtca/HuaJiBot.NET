using HuaJiBot.NET;
using HuaJiBot.NET.Adapter.OneBot;
using HuaJiBot.NET.Adapter.Satori;
using HuaJiBot.NET.Adapter.Kook;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Config;
using HuaJiBot.NET.Logger;

//using HuaJiBot.NET.Adapter.Red;
//BotService CreateRedProtocolService()
//{
//    var token = File.ReadAllText(
//        Path.Combine(
//            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//            "BetterUniverse",
//            "QQNT",
//            "RED_PROTOCOL_TOKEN"
//        )
//    ); //读取密钥
//    var api = new RedProtocolAdapter("localhost:16530", token); //链接协议适配器
//    return api;
//}
var logger = new ConsoleLogger();
BotServiceBase CreateOneBotService(Config config)
{
    var api = new OneBotAdapter(config.OneBot.Url, config.OneBot.Token) { Logger = logger }; //链接协议适配器
    return api;
}

BotServiceBase CreateSatoriService(Config config)
{
    var api = new SatoriAdapter(config.Satori.Url, config.Satori.Token) { Logger = logger }; //链接协议适配器
    return api;
}

BotServiceBase CreateKookService(Config config)
{
    var api = new KookAdapter(config.Kook.Token) { Logger = logger }; //链接协议适配器
    return api;
}

BotServiceBase CreateService(Config config)
{
    return config.Service switch
    {
        Config.ServiceType.OneBot => CreateOneBotService(config),
        Config.ServiceType.Satori => CreateSatoriService(config),
        Config.ServiceType.Kook => CreateKookService(config),
        _ => throw new NotSupportedException("不支持的协议类型"),
    };
}
Console.WriteLine("运行路径：" + Environment.CurrentDirectory);
var config = Config.Load(); //配置文件
config.Save();
var api = CreateService(config); //创建协议适配器
await Internal.SetupServiceAsync(api, config); //协议适配器
var accountId = ""; //账号
api.Events.OnBotLogin += (_, eventArgs) =>
{
    api.Log(
        string.IsNullOrWhiteSpace(eventArgs.ClientVersion)
            ? $"已连接到 {eventArgs.ClientName} 账号 {string.Join(",", eventArgs.Accounts)}"
            : $"已连接到 {eventArgs.ClientName} @ {eventArgs.ClientVersion} 账号 {string.Join(",", eventArgs.Accounts)}"
    );
    accountId = eventArgs.Accounts.FirstOrDefault();
};
var pluginDir = Path.Combine(Environment.CurrentDirectory, "plugins"); //插件目录
#region 额外插件
//复制额外插件（主要开发使用）
if (config.ExtraPlugins is { Length: > 0 } extraPluginsList)
{
    var extraPlugins = Path.Combine(pluginDir, "extra");

    if (Directory.Exists(extraPlugins))
        Directory.Delete(extraPlugins, true);
    Directory.CreateDirectory(extraPlugins);
    foreach (var file in extraPluginsList)
    {
        if (File.Exists(file))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".dll"))
            {
                File.Copy(file, Path.Combine(extraPlugins, fileName), true);
                api.Log($"复制额外插件 {fileName} 成功。");
            }
        }
        else
        {
            api.Warn($"额外插件 {file} 不存在。");
        }
    }
}
#endregion
await Internal.SetupAsync(api, pluginDir); //启动
while (true)
{
    if (Console.ReadLine() is { } line)
    {
        var cmds = line.Split(' ');
        switch (cmds)
        {
            case ["quit" or "q"]:
                break;
            case ["r" or "rc"]:
                api.Reconnect();
                break;
            case ["save"]:
                var result = api.Config.Save();
                api.Log("配置文件保存成功：" + result);
                break;
            case ["send", var targetGroup, var message]:
                await api.SendGroupMessageAsync(accountId, targetGroup, message);
                break;
            default:
                Console.WriteLine($"未知的命令 {line} .");
                break;
        }
    }
}
