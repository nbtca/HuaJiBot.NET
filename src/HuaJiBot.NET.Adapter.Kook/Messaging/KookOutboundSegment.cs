using Kook;

namespace HuaJiBot.NET.Adapter.Kook.Messaging;

/// <summary>
/// 一段待发送的 KOOK 内容。
/// <paramref name="Quote"/> 表示该段发送时携带的引用（引用只作用于其后的第一条消息）。
/// </summary>
internal abstract record KookOutboundSegment(IQuote? Quote);
