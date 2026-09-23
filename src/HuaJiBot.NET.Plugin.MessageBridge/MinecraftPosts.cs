using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Plugin.MessageBridge;

internal static class MinecraftPosts
{
    internal static Post Chat(string player, string message, string fallback) =>
        new($"**{Post.Escape(player)}**: {Post.Escape(message)}", fallback);

    internal static Post Join(string player, string fallback) =>
        new($"➕ {Post.Escape(player)} 加入了服务器", fallback) { Silent = true };

    internal static Post Quit(string player, string fallback) =>
        new($"➖ {Post.Escape(player)} 离开了服务器", fallback) { Silent = true };

    internal static Post Death(string message, string fallback) =>
        new($"💀 {Post.Escape(message)}", fallback) { Silent = true };

    internal static Post Advancement(string player, string name, string? description, string fallback) =>
        new(
            $"🏆 **{Post.Escape(player)}** 完成了进度 **{Post.Escape(name)}**"
                + (string.IsNullOrWhiteSpace(description) ? "" : $"\n*{Post.Escape(description)}*"),
            fallback
        )
        {
            Silent = true,
        };
}
