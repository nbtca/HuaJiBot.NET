using System;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET;

public class Global
{
    internal static BotServiceBase? ServiceInstance { private get; set; }
    public static BotServiceBase Service =>
        ServiceInstance ?? throw new InvalidOperationException("Service is not set up.");
}
