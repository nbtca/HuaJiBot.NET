using SixLabors.Fonts;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Utils;

public static class IconFonts
{
    private static readonly Lazy<FontFamily> IcoMoonInstance =
        new(() =>
        {
            using var s = new MemoryStream(Assets.icomoon);
            var collection = new FontCollection();
            collection.Add(s);
            var font = collection.Families.ToArray()[0];
            return font;
        });
    public static FontFamily IcoMoon => IcoMoonInstance.Value;

    public static Font IcoMoonFont(float size) => IcoMoon.CreateFont(size);

    public static char IconLaw => 'a';
    public static char IconPulls => 'b';
    public static char IconIssues => 'c';
    public static char IconForks => 'd';
    public static char IconStars => 'e';
    public static char IconCircle => 'f';
    public static char IconCommit => 'g';
    public static char ActionSkip => 'h';
    public static char IssueClosed => 'i';
    public static char IssueOpened => 'j';
}
