using HuaJiBot.NET;
using HuaJiBot.NET.Adapter.Red;
using HuaJiBot.NET.CLI.Config;

var config = Config.Load(); //配置文件
config.Save();

var token = File.ReadAllText(
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BetterUniverse",
        "QQNT",
        "RED_PROTOCOL_TOKEN"
    )
); //读取密钥
var api = new RedProtocolAdapter("ws://localhost:16530", token); //链接协议适配器
await Internal.SetupService(api); //协议适配器
var accountId = ""; //账号
api.Events.OnBotLogin += (_, eventArgs) =>
{
    Console.WriteLine(
        $"已链接到{eventArgs.ClientName}@{eventArgs.ClientVersion} 账号{eventArgs.AccountId}"
    );
    accountId = eventArgs.AccountId;
};
Internal.Setup(); //启动
Console.WriteLine("setup");
while (true)
{
    var line = Console.ReadLine();
    var cmds = line.Split(' ');
    switch (cmds)
    {
        case ["quit" or "q"]:
            break;
        case ["send", var targetGroup, var message]:
            api.SendGroupMessage(accountId, targetGroup, message);
            break;
        default:
            Console.WriteLine($"未知的命令{line}");
            break;
    }
}
