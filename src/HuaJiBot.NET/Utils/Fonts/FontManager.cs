using SixLabors.Fonts;

namespace HuaJiBot.NET.Utils.Fonts;

public static class FontManager
{
    public class FontLoader
    {
        private readonly Lazy<FontFamily> _instance;

        public FontLoader(Func<byte[]> getBytes)
        {
            _instance = new(() =>
            {
                using var s = new MemoryStream(getBytes());
                var collection = new FontCollection();
                collection.Add(s);
                var font = collection.Families.ToArray()[0];
                return font;
            });
        }

        public FontFamily Font => _instance.Value;

        public Font CreateFont(float size) => Font.CreateFont(size);
    }

    //public static FontLoader ComicMono => new(() => Assets.ComicMono);
    public static FontLoader ComicNeue => new(() => Assets.ComicNeue_Bold);
    public static FontLoader MaoKenTangYuan => new(() => Assets.MaoKenTangYuan);
}
