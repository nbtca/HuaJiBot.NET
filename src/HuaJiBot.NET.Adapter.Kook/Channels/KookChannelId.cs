using System.Diagnostics.CodeAnalysis;

namespace HuaJiBot.NET.Adapter.Kook.Channels;

/// <summary>
/// 结构化的 KOOK 频道标识：由构造保证"私聊 / 群聊"在类型上显式可辨，
/// 取代散落各处的 ulong / Guid 解析推断。
/// </summary>
internal readonly record struct KookChannelId
{
    private KookChannelId(KookChannelKind kind, ulong guildChannelId)
    {
        Kind = kind;
        GuildChannelId = guildChannelId;
    }

    private KookChannelId(KookChannelKind kind, Guid directChatCode)
    {
        Kind = kind;
        DirectChatCode = directChatCode;
    }

    public KookChannelKind Kind { get; }

    public ulong GuildChannelId { get; }

    public Guid DirectChatCode { get; }

    public static KookChannelId Guild(ulong channelId) =>
        new(KookChannelKind.Guild, channelId);

    public static KookChannelId Direct(Guid chatCode) =>
        new(KookChannelKind.Direct, chatCode);

    /// <summary>
    /// 服务器文字频道 ID 为十进制 ulong，私聊 chatCode 为 Guid。
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? raw, out KookChannelId id)
    {
        if (ulong.TryParse(raw, out var channelId))
        {
            id = Guild(channelId);
            return true;
        }
        if (Guid.TryParse(raw, out var chatCode))
        {
            id = Direct(chatCode);
            return true;
        }
        id = default;
        return false;
    }
}
