using System.ComponentModel;
using System.Text.RegularExpressions;
using Figgle;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

/// <summary>Deterministic TAAG-style FIGlet rendering for the bundled ANSI Shadow font.</summary>
public sealed partial class AsciiArtTools
{
    private const int MaxInputLength = 24;
    private static readonly Lazy<FiggleFont> AnsiShadow = new(() =>
    {
        using var stream = typeof(AsciiArtTools).Assembly.GetManifestResourceStream(
            "HuaJiBot.NET.Plugin.AIChat.Fonts.ANSI Shadow.flf"
        ) ?? throw new InvalidOperationException("ANSI Shadow font resource is missing.");
        return FiggleFontParser.Parse(stream);
    });

    [GeneratedRegex(
        @"^\s*TAAG\s*(?:处理|渲染|生成)\s*[""“「]?(?<text>[^,，""”」\r\n]{1,40})[""”」]?\s*[,，]\s*使用\s*(?<font>[^,，\r\n]{1,40})(?:\s*[,，].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex TaagCommandRegex();

    public static bool TryRenderCommand(string command, out string result)
    {
        var match = TaagCommandRegex().Match(command);
        if (!match.Success)
        {
            result = string.Empty;
            return false;
        }

        var font = match.Groups["font"].Value.Trim();
        if (!font.Equals("ANSI Shadow", StringComparison.OrdinalIgnoreCase)
            && !font.Equals("ANSI-Shadow", StringComparison.OrdinalIgnoreCase))
        {
            result = "目前仅支持 ANSI Shadow 字体。";
            return true;
        }

        try
        {
            result = new AsciiArtTools().RenderAnsiShadow(match.Groups["text"].Value.Trim());
        }
        catch (ArgumentException exception)
        {
            result = exception.Message;
        }
        return true;
    }

    [Description("使用 TAAG 的 ANSI Shadow 字体将短英文文本渲染为准确的多行字符画。需要字符画时调用此工具，不要自己编造字形。")]
    public string RenderAnsiShadow(
        [Description("要渲染的英文或 ASCII 文本，最多 24 个字符。")]
            string text
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("请输入要渲染的文字。", nameof(text));
        text = text.Trim();
        if (text.Length > MaxInputLength || text.Any(c => c is < ' ' or > '~'))
            throw new ArgumentException("仅支持最多 24 个 ASCII 字符。", nameof(text));

        return AnsiShadow.Value.Render(text).TrimEnd('\r', '\n');
    }
}
