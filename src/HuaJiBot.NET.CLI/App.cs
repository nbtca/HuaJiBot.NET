using HuaJiBot.NET;
using HuaJiBot.NET.Adapter.Red;
using HuaJiBot.NET.CLI.Config;

Internal.SetupService(new RedProtocolAdapter()); //协议适配器
var api = Global.Service; //获取服务实例
var config = Config.Load(); //配置文件

Console.WriteLine("setup");
Console.ReadLine();
