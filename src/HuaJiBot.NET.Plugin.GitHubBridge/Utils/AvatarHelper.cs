namespace HuaJiBot.NET.Plugin.GitHubBridge.Utils;

internal static class AvatarHelper
{
    public static async Task<byte[]> Get(string avatarUrl)
    {
        try
        {
            using HttpClient client = new();
            return await client.GetByteArrayAsync(avatarUrl);
        }
        catch (Exception)
        {
            Console.WriteLine("get avatar failed");
            Console.WriteLine(avatarUrl);
            using HttpClient client = new();
            return await client.GetByteArrayAsync("https://i.nbtca.space/favicon.png");
        }
    }
}
