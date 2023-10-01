using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET;

public static class Internal
{
    public static void SetupService<T>(T service)
        where T : BotServiceBase
    {
        Global.ServiceInstance = service;
    }
}
