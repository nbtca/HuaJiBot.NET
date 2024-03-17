using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Adapter.OneBot;

internal class OneBotMessageHandler(BotServiceBase service, Action<string> send)
{
    public async Task ProcessMessageAsync(string message)
    {
        service.LogDebug(message);
    }
}
