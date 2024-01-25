using SixLabors.Fonts;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Utils;

public class TextRun(string text, Color color, Font? font = null)
{
    public TextRun(string text, Font? font = null)
        : this(text, Color.White, font) { }

    public TextRun(char text, Color color, Font? font = null)
        : this(text.ToString(), color, font) { }

    public TextRun(char text, Font? font = null)
        : this(text.ToString(), Color.White, font) { }

    public static implicit operator TextRun(string text) => new(text, Color.White, null);

    public string Text { get; init; } = text.Replace("\r", null);
    public Color Color { get; init; } = color;
    public Font? Font { get; init; } = font;

    public void Deconstruct(out string t, out Color c, out Font? font)
    {
        t = Text;
        c = Color;
        font = Font;
    }
}
