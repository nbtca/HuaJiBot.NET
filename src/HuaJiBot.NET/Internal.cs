using System;
using System.Linq;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET;

public static class Internal
{
    public static void SetupService<T>(T service)
        where T : BotServiceBase
    {
        Global.ServiceInstance = service;
    }

    /// <summary>
    /// 启动并加载插件
    /// </summary>
    public static void Setup()
    {
        //加载插件


        //响应启动事件
        Events.Events.CallOnStartup();
    }
}
