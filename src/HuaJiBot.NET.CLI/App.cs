using HuaJiBot.NET;
using HuaJiBot.NET.Adapter.OneBot;
using HuaJiBot.NET.Adapter.Satori;
using HuaJiBot.NET.Adapter.Telegram;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Config;
using HuaJiBot.NET.Interfaces;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.PluginManager;
using Microsoft.Extensions.DependencyInjection;

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
Console.WriteLine("运行路径：" + Environment.CurrentDirectory);
var config = Config.Load(); //配置文件
config.Save();
var services = new ServiceCollection();

// Register logger
services.AddSingleton<ILogger>(logger);

// Register config
services.AddSingleton(config);

// Register adapter (via factory)
services.AddSingleton<BotServiceBase>(sp =>
{
    var cfg = sp.GetRequiredService<Config>();
    var lg = sp.GetRequiredService<ILogger>();
    return cfg.Service switch
    {
        Config.ServiceType.OneBot => new OneBotAdapter(cfg.OneBot.Url, cfg.OneBot.Token) { Logger = lg },
        Config.ServiceType.Satori => new SatoriAdapter(cfg.Satori.Url, cfg.Satori.Token) { Logger = lg },
        Config.ServiceType.Telegram => new TelegramAdapter(cfg.Telegram.Token) { Logger = lg },
        _ => throw new NotSupportedException("不支持的协议类型"),
    };
});
services.AddSingleton<IPluginService>(sp => sp.GetRequiredService<BotServiceBase>());
services.AddSingleton<IAdapterService>(sp => sp.GetRequiredService<BotServiceBase>());

// Register Internal
services.AddSingleton<Internal>();


using var provider = services.BuildServiceProvider();
var api = provider.GetRequiredService<BotServiceBase>(); //创建协议适配器
var adapterApi = provider.GetRequiredService<IAdapterService>();
var internalService = provider.GetRequiredService<Internal>();
await internalService.SetupServiceAsync(api, config); //协议适配器
var pluginManager = new PluginManager();
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
await pluginManager.SetupAsync(api, pluginDir); //启动
bool hasTty;
try
{
    // Try to get cursor position to determine whether we are attached to a real TTY.
    // Console.GetCursorPosition throws when output is redirected, so this is a good probe.
    _ = Console.GetCursorPosition();

    // Also ensure input/output are not redirected
    hasTty = !Console.IsInputRedirected && !Console.IsOutputRedirected;
}
catch
{
    hasTty = false;
}

if (hasTty)
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
                    adapterApi.Reconnect();
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
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
Console.WriteLine("Running... Press Ctrl+C to exit.");
await Task.Delay(Timeout.Infinite, cts.Token);
pluginManager.Shutdown(api);
Console.WriteLine("Exiting gracefully...");
