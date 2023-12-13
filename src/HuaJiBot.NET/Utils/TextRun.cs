using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Utils;

public class TextRun(string text, Color color)
{
    public TextRun(string text)
        : this(text, Color.White) { }

    public static implicit operator TextRun(string text) => new(text, Color.White);

    public string Text { get; init; } = text.Replace("\r", null);
    public Color Color { get; init; } = color;

    public void Deconstruct(out string t, out Color c)
    {
        t = Text;
        c = Color;
    }
}
