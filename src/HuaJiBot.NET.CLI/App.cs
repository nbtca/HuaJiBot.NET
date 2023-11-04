using HuaJiBot.NET;
using HuaJiBot.NET.Adapter.Red;
using HuaJiBot.NET.CLI.Config;

Internal.SetupService(new RedProtocolAdapter()); //协议适配器
var api = Global.Service; //获取服务实例
var config = Config.Load(); //配置文件
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
