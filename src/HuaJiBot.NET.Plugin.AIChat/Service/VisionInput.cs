using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.AI;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

/// <summary>Downloads bounded image attachments without exposing URLs or image data in logs.</summary>
internal sealed class VisionInput
{
    private static readonly HttpClient SharedClient = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectCallback = async (context, cancellationToken) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
                throw new VisionInputException("图片地址必须指向公共网络。");
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
        UseProxy = false,
    })
    { Timeout = TimeSpan.FromSeconds(20) };

    private readonly HttpClient _client;
    internal VisionInput(HttpClient? client = null) => _client = client ?? SharedClient;

    internal static ChatMessage CreateMessage(string? text, IEnumerable<string>? images = null)
    {
        var urls = images?.Distinct(StringComparer.Ordinal).ToArray() ?? [];
        var message = new ChatMessage(ChatRole.User,
            urls.Length > 0 && string.IsNullOrWhiteSpace(text) ? "请描述这张图片中的内容。" : text ?? "");
        foreach (var url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")
                || !string.IsNullOrEmpty(uri.UserInfo))
                throw new VisionInputException("图片附件没有可用的 HTTP(S) 地址。");
            message.Contents.Add(new UriContent(uri, "image/*"));
        }
        return message;
    }

    internal async Task PrepareAsync(IList<ChatMessage> messages, bool supportsVision,
        int maxImages, int maxBytes, CancellationToken cancellationToken)
    {
        var imageCount = messages.Sum(message => message.Contents.OfType<UriContent>().Count());
        if (imageCount == 0)
            return;
        if (!supportsVision)
            throw new VisionInputException("当前模型未启用图片理解，请用文字描述图片，或联系管理员启用视觉模型。");
        if (maxImages is < 1 or > 16 || maxBytes is < 1 or > 64 * 1024 * 1024)
            throw new VisionInputException("图片限制配置无效，请联系管理员。");
        if (imageCount > maxImages)
            throw new VisionInputException($"本次对话最多读取 {maxImages} 张图片，请减少附图或开始新的提问。");
        foreach (var message in messages)
        {
            var urls = message.Contents.OfType<UriContent>().ToArray();
            foreach (var content in urls)
                message.Contents.Remove(content);
            await AddImagesAsync(message, urls.Select(content => content.Uri.ToString()), maxBytes, cancellationToken);
        }
    }

    internal async Task AddImagesAsync(
        ChatMessage message, IEnumerable<string> urls, int maxBytes, CancellationToken cancellationToken)
    {
        foreach (var url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")
                || !string.IsNullOrEmpty(uri.UserInfo))
                throw new VisionInputException("图片附件没有可用的 HTTP(S) 地址。");
            try
            {
                using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new VisionInputException("图片无法下载，请重新发送图片。");
                if (response.Content.Headers.ContentLength > maxBytes)
                    throw new VisionInputException("图片超过大小限制，请压缩后重试。");
                using var downloadTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                downloadTimeout.CancelAfter(TimeSpan.FromSeconds(20));
                await using var stream = await response.Content.ReadAsStreamAsync(downloadTimeout.Token);
                using var data = new MemoryStream();
                var buffer = new byte[8192];
                int read;
                while ((read = await stream.ReadAsync(buffer, downloadTimeout.Token)) != 0)
                {
                    if (data.Length + read > maxBytes)
                        throw new VisionInputException("图片超过大小限制，请压缩后重试。");
                    data.Write(buffer, 0, read);
                }
                var bytes = data.ToArray();
                var format = Image.DetectFormat(bytes);
                if (format.DefaultMimeType is not ("image/png" or "image/jpeg" or "image/webp" or "image/gif"))
                    throw new VisionInputException("暂不支持该图片格式，请发送 PNG、JPEG、WebP 或 GIF。");
                message.Contents.Add(new DataContent(bytes, format.DefaultMimeType));
            }
            catch (VisionInputException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or UnknownImageFormatException or InvalidImageContentException)
            {
                throw new VisionInputException("图片读取失败或超时，请重新发送图片。");
            }
        }
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address))
            return false;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return (bytes[0] & 0xe0) == 0x20; // Global unicast only.
        return bytes[0] is not (0 or 10 or 127) && bytes[0] < 224
            && !(bytes[0] == 169 && bytes[1] == 254)
            && !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            && !(bytes[0] == 192 && bytes[1] == 168)
            && !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
    }
}

internal sealed class VisionInputException(string message) : Exception(message);
