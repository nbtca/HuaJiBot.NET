using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Config;

namespace HuaJiBot.NET;

public static class Internal
{
    public static Task SetupServiceAsync<T>(T service, Config.Config config)
        where T : BotService
    {
        Utils.NetworkTime.TryUpdateTimeDiff();
        service.Config = new ConfigWrapper(config);
        return service.SetupServiceAsync();
        //Global.ServiceInstance = service;
    }
}
