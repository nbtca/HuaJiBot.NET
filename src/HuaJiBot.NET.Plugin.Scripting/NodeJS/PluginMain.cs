namespace HuaJiBot.NET.Plugin.Scripting.NodeJS;

public class PluginMain : PluginBase
{
    //interface IConsole
    //{
    //    void Log(string message);
    //}

    protected override void Initialize()
    {
        //foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        //{
        //    Console.WriteLine(module.ModuleName);
        //}
        //var libnodePath = Path.Combine(
        //    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        //    "libnode"
        //);
        //var nodejs = new NodejsPlatform(libnodePath).CreateEnvironment();
        //nodejs.Run(() =>
        //{
        //    var console = nodejs.Import("global", "console");
        //    console.CallMethod("log", "Hello from JS!");
        //});
        //Info("启动成功！");
    }

    protected override void Unload() { }
}
