using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Config;

namespace HuaJiBot.NET;

public static class Internal
{
    public static async Task SetupServiceAsync<T>(T service, Config.Config config)
        where T : BotServiceBase
    {
        await Utils.NetworkTime.UpdateDiffAsync();
        service.Config = new ConfigWrapper(config);
        await service.SetupServiceAsync();
        //Global.ServiceInstance = service;
    }

    /// <summary>
    /// 插件列表
    /// </summary>
    internal static readonly List<(EntryPointBase entryPoint, PluginBase instance)> Plugins = new();

    /// <summary>
    /// 启动并加载插件
    /// </summary>
    /// <param name="api">接口实例</param>
    /// <param name="pluginDirectory">插件目录</param>
    public static async Task SetupAsync(BotServiceBase api, string pluginDirectory = "plugins")
    {
        //插件目录
        DirectoryInfo directoryInfo = new(pluginDirectory);
        if (!directoryInfo.Exists) //不存在则创建这个目录
        {
            directoryInfo.Create();
            api.Log("创建目录");
        }
        LoadAllPlugins(api, directoryInfo); //加载所有插件，并添加到Plugins列表
        //注入读取到的配置文件到对应结构
        foreach (var (_, plugin) in Plugins)
        { //遍历所有插件
            [MethodImpl(MethodImplOptions.Synchronized)]
            bool TryGetPluginConfig([NotNullWhen(true)] out ConfigBase? config)
            {
                foreach (var it in plugin.GetType().GetInterfaces())
                {
                    foreach (var m in it.GetProperties())
                    {
                        if (m.PropertyType.IsAssignableTo(typeof(ConfigBase)))
                        {
                            config = m.GetValue(plugin) as ConfigBase;

                            if (config is null)
                            {
#if DEBUG
                                api.Log(
                                    plugin.GetType()
                                        + "config = m.GetValue(plugin) as ITranslationMainLoader == null"
                                );
#endif
                                return false;
                            }
                            return true;
                        }
                    }
                }
                config = null;
                return false;
            }

            if (TryGetPluginConfig(out var config))
            { //取出ConfigKey和ConfigObject，将对应的配置注入到ConfigObject
                api.Config.Populate(plugin.Name, config);
            }
        }
        //保存配置
        api.Config.Save();
        //触发启动事件
        Events.Events.CallOnStartup(api);
        foreach (var (entryPoint, plugin) in Plugins) //调用所有插件的初始化方法
        {
            api.Log($"开始加载插件 {entryPoint.Name} 描述：{entryPoint.Description}");
            var sw = Stopwatch.StartNew(); //计时
            api.LoadAddCommand(plugin); //加载命令
            // ReSharper disable once MethodHasAsyncOverload
            plugin.Initialize(); //同步初始化
            await plugin.InitializeAsync(); //异步初始化
            sw.Stop(); //停止计时
            api.Log($"加载插件 {entryPoint.Name} 完成，耗时 {sw.ElapsedMilliseconds} ms");
        }
        //触发插件初始化完成事件
        Events.Events.CallOnInitialized(api);
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public static void Shutdown(BotServiceBase api)
    {
        //响应关闭事件
        Events.Events.CallOnShutdown(api);
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
            foreach (var entryPoint in assembly.GetCustomAttributes<EntryPointBase>()) //遍历所有EntryPoint注解
            {
                Plugins.Add((entryPoint, entryPoint.CreateInstance(api)));
                api.Log(
                    $"路径 {Path.GetRelativePath(Environment.CurrentDirectory, file.FullName)} 获取到插件 {entryPoint.Name}."
                );
            }
        }
        //foreach (var directory in directoryInfo.EnumerateDirectories())
        //{ //递归加载子目录的所有dll
        //    LoadAllPlugins(directory);
        //}
    }
}
