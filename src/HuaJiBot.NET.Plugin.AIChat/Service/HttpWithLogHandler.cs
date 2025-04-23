using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.AIChat.Config;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

class HttpWithLogHandler(BotService plugin, ModelConfig modelConfig) : HttpClientHandler
{
    protected override HttpResponseMessage Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (modelConfig.Logging)
            plugin.Log(
                $"请求：{request.Method} {request.RequestUri}\n{request.Headers}\n{request.Content}"
            );
        return base.Send(request, cancellationToken);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (modelConfig.Logging)
            plugin.Log(
                $"请求：{request.Method} {request.RequestUri}\n{request.Headers}\n{request.Content}"
            );
        return base.SendAsync(request, cancellationToken);
    }
}