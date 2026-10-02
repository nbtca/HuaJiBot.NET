using Kook;

namespace HuaJiBot.NET.Adapter.Kook.Messaging;

/// <summary>
/// 单张图片附件。
/// </summary>
internal sealed record KookImageSegment(string ImagePath, IQuote? Quote)
    : KookOutboundSegment(Quote);
