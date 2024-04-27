using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Utils;

public static class AvatarHelper
{
    private static string GetAvatarFileName(string avatarUrl)
    { //get md5
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(avatarUrl));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant() + ".png";
    }

    private static string GetAvatarCachePath(string key)
    {
        var dir = Path.Combine("cache", "avatar");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, key);
    }

    private static bool GetFromCache(string name, [NotNullWhen(true)] out byte[]? bytes)
    {
        var path = GetAvatarCachePath(name);
        if (File.Exists(path))
        {
            bytes = File.ReadAllBytes(path);
            return true;
        }
        bytes = null;
        return false;
    }

    private static void SaveToCache(string name, byte[] bytes)
    {
        var path = GetAvatarCachePath(name);
        File.WriteAllBytes(path, bytes);
    }

    public static async Task<byte[]> GetAsync(string avatarUrl)
    {
        var name = GetAvatarFileName(avatarUrl);
        try
        {
            if (GetFromCache(name, out var result))
            {
                return result;
            }
        }
        catch
        {
            // ignored
        }
        try
        {
            using HttpClient client = new();
            var result = await client.GetByteArrayAsync(avatarUrl);
            SaveToCache(name, result);
            return result;
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
