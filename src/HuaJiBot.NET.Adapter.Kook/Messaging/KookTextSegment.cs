using Kook;

namespace HuaJiBot.NET.Adapter.Kook.Messaging;

/// <summary>
/// 合并后的一段 KMarkdown 文本。
/// </summary>
internal sealed record KookTextSegment(string Markdown, IQuote? Quote) : KookOutboundSegment(Quote);
