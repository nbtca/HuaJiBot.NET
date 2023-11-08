using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    internal static readonly List<(EntryPointBase entryPoint, PluginBase instance)> Plugins = new();

    /// <summary>
    /// 启动并加载插件
    /// </summary>
    /// <param name="pluginDirectory">插件目录</param>
    public static async Task Setup(BotServiceBase api, string pluginDirectory = "plugins")
    {
        //插件目录
        DirectoryInfo directoryInfo = new(pluginDirectory);
        if (!directoryInfo.Exists) //不存在则创建这个目录
        {
            directoryInfo.Create();
            api.Log("创建目录");
        }
        LoadAllPlugins(api, directoryInfo); //加载所有插件，并添加到Plugins列表
        //触发启动事件
        Events.Events.CallOnStartup();
        foreach (var (entryPoint, plugin) in Plugins) //调用所有插件的初始化方法
        {
            api.Log($"开始加载插件 {entryPoint.Name} 描述：{entryPoint.Description}");
            var sw = Stopwatch.StartNew(); //计时
            await plugin.Initialize(); //异步初始化
            sw.Stop(); //停止计时
            api.Log($"加载插件 {entryPoint.Name} 完成，耗时 {sw.ElapsedMilliseconds} ms");
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
        foreach (var (_, plugin) in Plugins) //调用所有插件的卸载方法
        {
            plugin.Unload();
        }
        Plugins.Clear(); //清空插件列表
    }

    private static void LoadAllPlugins(BotServiceBase api, DirectoryInfo directoryInfo)
    {
        var libsDir = Path.Combine(directoryInfo.FullName, "libs");
        if (!Directory.Exists(libsDir))
            Directory.CreateDirectory(libsDir);
        var libs = Directory.GetFiles(libsDir, "*.dll", SearchOption.AllDirectories); //获取所有依赖库
        foreach (var lib in libs)
        {
            api.Log("加载依赖库：" + Path.GetRelativePath(Environment.CurrentDirectory, lib));
            Assembly.LoadFrom(lib); //加载依赖库
        }
        foreach (
            var file in directoryInfo
                .EnumerateFiles("*.dll", SearchOption.AllDirectories) //遍历所有dll文件
                //SearchOption.AllDirectories 包括所有子目录
                .SkipWhile(x => libs.Contains(x.FullName)) //跳过libs目录下的dll
        )
        {
            api.Log("加载动态链接库：" + Path.GetRelativePath(Environment.CurrentDirectory, file.FullName));
            var assembly = Assembly.LoadFrom(file.FullName); //加载程序集
            foreach (var module in assembly.Modules)
            {
                foreach (var entryPoint in module.GetCustomAttributes<EntryPointBase>()) //遍历所有EntryPoint注解
                {
                    Plugins.Add((entryPoint, entryPoint.Instance));
                    api.Log(
                        $"路径 {Path.GetRelativePath(Environment.CurrentDirectory, file.FullName)} 获取到插件 {entryPoint.Name}."
                    );
                }
            }
        }
        //foreach (var directory in directoryInfo.EnumerateDirectories())
        //{ //递归加载子目录的所有dll
        //    LoadAllPlugins(directory);
        //}
    }
}
