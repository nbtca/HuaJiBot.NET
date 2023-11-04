using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET;

public static class Internal
{
    public static async Task SetupService<T>(T service)
        where T : BotServiceBase
    {
        await service.SetupService();
        Global.ServiceInstance = service;
    }

    /// <summary>
    /// 插件列表
    /// </summary>
    internal static readonly List<PluginBase> Plugins = new();

    /// <summary>
    /// 启动并加载插件
    /// </summary>
    /// <param name="pluginDirectory">插件目录</param>
    public static void Setup(string pluginDirectory = "plugins")
    {
        //插件目录
        DirectoryInfo directoryInfo = new(pluginDirectory);
        if (!directoryInfo.Exists) //不存在则创建这个目录
            directoryInfo.Create();
        LoadAllPlugins(directoryInfo); //加载所有插件，并添加到Plugins列表

        //触发启动事件
        Events.Events.CallOnStartup();
        foreach (var plugin in Plugins) //调用所有插件的初始化方法
        {
            plugin.Initialize();
        }
        //触发插件初始化完成事件
        Events.Events.CallOnInitialized();
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public static void Shutdown()
    {
        //响应关闭事件
        Events.Events.CallOnShutdown();
        foreach (var plugin in Plugins) //调用所有插件的卸载方法
        {
            plugin.Unload();
        }
        Plugins.Clear(); //清空插件列表
    }

    private static void LoadAllPlugins(DirectoryInfo directoryInfo)
    {
        foreach (var file in directoryInfo.EnumerateFiles("*.dll")) //遍历所有dll文件
        {
            var assembly = Assembly.LoadFile(file.Name); //加载程序集
            foreach (var entryPoint in assembly.GetCustomAttributes<EntryPointBase>()) //遍历所有EntryPoint注解
            {
                Plugins.Add(entryPoint.Instance);
            }
        }
        foreach (var directory in directoryInfo.EnumerateDirectories())
        { //递归加载子目录的所有dll
            LoadAllPlugins(directory);
        }
    }
}
