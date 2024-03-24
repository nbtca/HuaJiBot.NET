using System.Diagnostics.CodeAnalysis;
using HuaJiBot.NET.Bot;
using Microsoft.VisualBasic;
using static HuaJiBot.NET.Commands.CommonCommandReader;

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
    public abstract bool At([NotNullWhen(true)] out string? id);
}

public class DefaultCommandReader(IEnumerable<ReaderEntity> msg) : CommonCommandReader
{
    public override IEnumerable<ReaderEntity> Msg => msg;
}

public abstract class CommonCommandReader : CommandReader
{
    public abstract record ReaderEntity
    {
        public static implicit operator ReaderEntity(string text) => new ReaderText(text);
    }

    public record ReaderText(string Text) : ReaderEntity;

    public record ReaderAt(string AtTarget) : ReaderEntity
    {
        public ReaderAt(string atTarget, string? atText)
            : this(atTarget)
        {
            if (atText is not null)
            {
                AtText = atText;
            }
        }

        public string AtText { get; init; } = $"@{AtTarget}";
    }

    private abstract record MatchResult
    {
        public static implicit operator MatchResult(string text) => new MatchText(text);
    };

    private record MatchText(string Text) : MatchResult;

    private record MatchAt(string AtTarget, string AtText) : MatchResult;

    private IEnumerable<string>? _currentExpected = null;
    public abstract IEnumerable<ReaderEntity> Msg { get; }
    private IEnumerator<MatchResult> _seq = null!;
    private bool _lastOne = false;

    public void Reset()
    {
        _seq = BuildReadSeq().GetEnumerator();
        IEnumerable<MatchResult> BuildReadSeq()
        {
            foreach (var s in Msg)
            {
                switch (s)
                {
                    case ReaderText { Text: var _text }:
                    {
                        var text = _text.TrimStart();
                        while (text.Length > 0)
                        {
                            if (_lastOne) //如果是最后一个参数
                            {
                                yield return text; //返回剩下的整个文本
                                //todo 处理并合并剩下的elements
                                yield break; //结束
                            }
                            else if (
                                //当前有期望的文本，如匹配枚举
                                _currentExpected?.FirstOrDefault(text.StartsWith) is
                                { } matchedExpected //当前文本以某个期望的文本开头
                            )
                            {
                                yield return matchedExpected; //返回匹配的文本
                                text = text[matchedExpected.Length..].TrimStart();
                            }
                            else
                            {
                                string quote;
                                if (
                                    (
                                        text.StartsWith(quote = "\"") //双引号开头
                                        || text.StartsWith(quote = "'") //单引号开头
                                        || text.StartsWith(quote = "```") //代码块
                                    )
                                    && text.IndexOf(quote, quote.Length, StringComparison.Ordinal)
                                        is not -1
                                            and var end //找结束的匹配引号
                                )
                                {
                                    //if (end == -1) //没有找到结束引号
                                    //{
                                    //    yield return text; //返回整个文本
                                    //    text = "";
                                    //}
                                    //else
                                    { //找到了结束引号
                                        //返回引号内的文本
                                        //var textWithQuote = text[..(end + quote.Length)];
                                        var textWithoutQuote = text[(quote.Length)..end];
                                        yield return textWithoutQuote;
                                        //剩下的文本进入下一轮循环
                                        text = text[(end + 1)..].TrimStart();
                                    }
                                }
                                else
                                { //不是引号开头(或者没有匹配的引号)
                                    //找到第一个空格
                                    end = text.IndexOf(' ');
                                    //如果没有找到空格，说明这是最后一个参数
                                    if (end == -1)
                                    {
                                        //返回整个文本
                                        yield return text;
                                        text = "";
                                    }
                                    else
                                    {
                                        //返回空格前的文本
                                        yield return text[..end];
                                        //剩下的文本进入下一轮循环
                                        text = text[end..].TrimStart();
                                    }
                                }
                            }
                        }
                        break;
                    }
                    case ReaderAt { AtTarget: var atTarget, AtText: var atText }:
                    {
                        yield return new MatchAt(atTarget, atText);
                        break;
                    }
                }
            }
        }
    }

    protected CommonCommandReader()
    {
        Reset();
    }

    public override bool Match(
        IEnumerable<string> expected,
        [NotNullWhen(true)] out string? matched,
        bool lastOne = false
    )
    {
        _lastOne = lastOne;
        _currentExpected = expected; //设置期望的参数
        var result = _seq.MoveNext(); //匹配下一个参数
        _currentExpected = null; //清空期望的参数
        if (result && _seq.Current is MatchText { Text: var v })
        {
            matched = v; //返回匹配到的参数
            return true;
        }
        matched = null;
        return false;
    }

    public override bool Input([NotNullWhen(true)] out string? text, bool lastOne = false)
    {
        _lastOne = lastOne;
        if (_seq.MoveNext())
        {
            switch (_seq.Current)
            {
                //向下匹配一个参数
                case MatchText { Text: var v }:
                    text = v;
                    return true;
                //向下匹配一个参数 把At当文本显示
                case MatchAt { AtText: var v2, }:
                    text = v2;
                    return true;
            }
        }
        text = null;
        return false;
    }

    public override bool At([NotNullWhen(true)] out string? id)
    {
        if (_seq.MoveNext() && _seq.Current is MatchAt { AtTarget: var v }) //向下匹配一个参数
        {
            id = v;
            return true;
        }
        id = null;
        return false;
    }
}
