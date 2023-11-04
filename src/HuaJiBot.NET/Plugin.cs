using System;
using System.IO;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET;

public abstract class PluginBase
{
    public BotServiceBase Service => Global.Service;

    /// <summary>
    /// 插件初始化
    /// </summary>
    protected internal abstract void Initialize();

    /// <summary>
    /// 插件卸载
    /// </summary>
    protected internal abstract void Unload();
}
