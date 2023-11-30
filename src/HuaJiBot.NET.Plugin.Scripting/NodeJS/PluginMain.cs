using System.Reflection;
using Microsoft.JavaScript.NodeApi;
using Microsoft.JavaScript.NodeApi.Runtime;

namespace HuaJiBot.NET.Plugin.Scripting.NodeJS;

public class PluginMain : PluginBase
{
    interface IConsole
    {
        void Log(string message);
    }

    protected override Task Initialize()
    {
        var libnodePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "libnode"
        );
        var nodejs = new NodejsPlatform(libnodePath).CreateEnvironment();
        nodejs.Run(() =>
        {
            var console = nodejs.Import("global", "console");
            console.CallMethod("log", "Hello from JS!");
        });
        Info("启动成功！");
        return Task.CompletedTask;
    }

    protected override void Unload() { }
}
