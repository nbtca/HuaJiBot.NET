namespace HuaJiBot.NET.Adapter.Kook.Channels;

internal enum KookChannelKind
{
    /// <summary>
    /// 服务器文字频道，标识为 ulong。
    /// </summary>
    Guild,

    /// <summary>
    /// 私聊频道，标识为 Guid（chatCode）。
    /// </summary>
    Direct,
}
