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

    private static bool GetFromCache(
        string name,
        [NotNullWhen(true)] out byte[]? bytes,
        out bool needUpdate
    )
    {
        var path = GetAvatarCachePath(name);
        var file = new FileInfo(path);
        if (file.Exists)
        {
            bytes = File.ReadAllBytes(path);
            needUpdate = DateTime.Now - file.LastWriteTime > TimeSpan.FromDays(7);
            return true;
        }
        bytes = null;
        needUpdate = false;
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
            if (GetFromCache(name, out var result, out var needUpdate))
            {
                if (needUpdate)
                {
                    try
                    {
                        return await TryUpdate();
                    }
                    catch
                    {
                        return result;
                    }
                }
                return result;
            }
        }
        catch
        {
            // ignored
        }
        try
        {
            return await TryUpdate();
        }
        catch (Exception)
        {
            Console.WriteLine("get avatar failed");
            Console.WriteLine(avatarUrl);
            using HttpClient client = new();
            return await client.GetByteArrayAsync("https://i.nbtca.space/favicon.png");
        }
        async Task<byte[]> TryUpdate()
        {
            using HttpClient client = new();
            var result = await client.GetByteArrayAsync(avatarUrl);
            SaveToCache(name, result);
            return result;
        }
    }
}
