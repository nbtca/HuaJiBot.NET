namespace HuaJiBot.NET.Adapter.OneBot.Message;

public static class CommonResolver
{
    private static readonly HttpClient Client = new();

    public static async Task<byte[]?> ResolveAsync(string url)
    {
        if (url.StartsWith("base64://"))
            return Convert.FromBase64String(url.Replace("base64://", ""));
        Uri uri = new(url);
        return uri.Scheme switch
        {
            "http" or "https" => await (await Client.GetAsync(uri)).Content.ReadAsByteArrayAsync(),
            "file" => await File.ReadAllBytesAsync(Path.GetFullPath(uri.LocalPath)),
            _ => null,
        };
    }

    public static async Task<Stream?> ResolveStreamAsync(string url)
    {
        if (url.StartsWith("base64://"))
            return new MemoryStream(Convert.FromBase64String(url.Replace("base64://", "")));
        Uri uri = new(url);
        return uri.Scheme switch
        {
            "http" or "https" => await (await Client.GetAsync(uri)).Content.ReadAsStreamAsync(),
            "file" => new FileStream(Path.GetFullPath(uri.LocalPath), FileMode.Open),
            _ => null,
        };
    }
}
