using SixLabors.Fonts;
using SixLabors.ImageSharp;

namespace HuaJiBot.NET.Utils;

public record TextRun
{
    public TextRun(string text, Color color, Font? font = null)
    {
        Text = text.Replace("\r", null);
        Color = color;
        Font = font;
    }

    public TextRun(string text, Font? font = null)
        : this(text, Color.White, font) { }

    public TextRun(char text, Color color, Font? font = null)
        : this(text.ToString(), color, font) { }

    public TextRun(char text, Font? font = null)
        : this(text.ToString(), Color.White, font) { }

    public static implicit operator TextRun(string text) => new(text, Color.White, null);

    public string Text { get; init; }
    public Color Color { get; init; }
    public Font? Font { get; init; }
    public bool Underline { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Strikethrough { get; init; }
    public bool Olive { get; init; }
    public int? FontSize { get; init; }

    //for quote
    public bool StartPrefix { get; init; }
    public bool EndPrefix { get; init; }
    public TextAttributes TextAttributes { get; init; }

    public void Deconstruct(out string t, out Color c, out Font? font)
    {
        t = Text;
        c = Color;
        font = Font;
    }
}
