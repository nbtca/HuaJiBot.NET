using HuaJiBot.NET;
using HuaJiBot.NET.Adapter.Red;
using HuaJiBot.NET.CLI.Config;

var token = File.ReadAllText(
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BetterUniverse",
        "QQNT",
        "RED_PROTOCOL_TOKEN"
    )
);
var api = new RedProtocolAdapter("ws://localhost:16530", token);
await Internal.SetupService(api); //协议适配器
var config = Config.Load(); //配置文件
config.Save();
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
        default:
            Console.WriteLine($"未知的命令{line}");
            break;
    }
}
