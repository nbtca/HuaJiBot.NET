using System.Text;
using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

public class HttpWithLogHandler(BotService plugin) : HttpClientHandler
{
    private string DecodeContent(HttpContent? content)
    {
        switch (content)
        {
            case StringContent stringContent:
                var str = stringContent.ReadAsStringAsync().Result;
                return str;
            case ByteArrayContent bytesContent:
                var bytes = bytesContent.ReadAsByteArrayAsync().Result;
                return Encoding.UTF8.GetString(bytes);
        }

        return "";
    }

    protected override HttpResponseMessage Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        plugin.Log(
            $"Request[{request.Method}] {request.RequestUri}\t{DecodeContent(request.Content)}"
        );
        return base.Send(request, cancellationToken);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        plugin.Log(
            $"Request[{request.Method}] {request.RequestUri}\t{DecodeContent(request.Content)}"
        );
        return base.SendAsync(request, cancellationToken);
    }
}
