namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class ShortLink
{
    internal static async Task<string> OrRawAsync(this PluginMain plugin, Uri url)
    {
        var raw = url.ToString();
        try
        {
            return (await plugin.ShortLinkApi.ShortLinkAsync(plugin.Config.ShortLinkApi, raw)).Url;
        }
        catch (Exception ex)
        {
            plugin.Error("Failed to shorten link", ex);
            return raw;
        }
    }
}
