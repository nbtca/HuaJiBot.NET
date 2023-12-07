using System.Diagnostics.CodeAnalysis;

namespace HuaJiBot.NET.Commands;

/// <summary>
/// 指令读取
/// 基类
/// </summary>
public abstract class CommandReader
{
    public abstract bool Match(
        IEnumerable<string> expected,
        [NotNullWhen(true)] out string? matched,
        bool lastOne = false
    );

    public bool Match<T>(
        IEnumerable<T> expectedEnums,
        Func<T, string> selector,
        [NotNullWhen(true)] out T? matched,
        bool lastOne = false
    )
    {
        return Match(expectedEnums, x => new[] { selector(x) }, out matched, lastOne);
    }

    public bool Match<T>(
        IEnumerable<T> expectedEnums,
        Func<T, IEnumerable<string>> selector,
        [NotNullWhen(true)] out T? matched,
        bool lastOne = false
    )
    {
        var array = expectedEnums as T[] ?? expectedEnums.ToArray();
        var allRequired = array.Select(selector).Aggregate((x, y) => x.Concat(y));
        var result = Match(allRequired, out var matchedString, lastOne);
        if (result)
        {
            matched = array.FirstOrDefault(x => selector(x).Contains(matchedString));
            return matched is not null;
        }
        matched = default;
        return false;
    }

    public abstract bool Input([NotNullWhen(true)] out string? text, bool lastOne = false);
    //public abstract bool At(out string id);
}
