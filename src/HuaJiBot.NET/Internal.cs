using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Config;
using HuaJiBot.NET.Interfaces;

namespace HuaJiBot.NET;

public class Internal
{
    public Internal() { }

    public Task SetupServiceAsync<T>(T service, Config.Config config)
        where T : BotService, IAdapterService
    {
        Utils.NetworkTime.TryUpdateTimeDiff();
        service.Config = new ConfigWrapper(config);
        IAdapterService adapterService = service;
        return adapterService.SetupServiceAsync();
        //Global.ServiceInstance = service;
    }
}
