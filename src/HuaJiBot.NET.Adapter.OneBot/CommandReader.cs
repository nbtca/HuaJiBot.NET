using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;

namespace HuaJiBot.NET.Adapter.Red;

/// <summary>
/// 指令读取
/// </summary>
internal class OneBotCommandReader : CommandReader
{
    private IEnumerable<string>? _currentExpected = null;
    private readonly List<MessageEntity> _msg;

    private readonly BotServiceBase _service;
    private IEnumerator<string> _seq = null!;
    private bool _lastOne = false;

    public void Reset()
    {
        _seq = BuildReadSeq().GetEnumerator();
        IEnumerable<string> BuildReadSeq()
        {
            foreach (var element in _msg)
            {
                switch (element)
                {
                    case TextMessageEntity { Text: not null and var text }: //文本消息
                        text = text.TrimStart();
                        while (text.Length > 0)
                        {
                            if (_lastOne) //如果是最后一个参数
                            {
                                yield return text; //返回整个文本
                                //todo 处理剩下的element
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
                                char quote;
                                if (
                                    text.StartsWith(quote = '"') //双引号开头
                                    || text.StartsWith(quote = '\'') //单引号开头
                                )
                                {
                                    var end = text.IndexOf(quote, 1); //找结束的匹配引号
                                    if (end == -1) //没有找到结束引号
                                    {
                                        yield return text; //返回整个文本
                                        text = "";
                                    }
                                    else
                                    { //找到了结束引号
                                        //返回引号内的文本
                                        yield return text[..(end + 1)];
                                        //剩下的文本进入下一轮循环
                                        text = text[(end + 1)..].TrimStart();
                                    }
                                }
                                else
                                { //不是引号开头
                                    //找到第一个空格
                                    var end = text.IndexOf(' ');
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
                    default: //todo impl other cases of elements
                        _service.LogDebug("Not impl of element type: " + element.GetType().Name);
                        break;
                }
            }
        }
    }

    public OneBotCommandReader(BotServiceBase api, List<MessageEntity> msg)
    {
        _msg = msg;
        _service = api;
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
        if (result)
        {
            matched = _seq.Current; //返回匹配到的参数
            return true;
        }
        matched = null;
        return false;
    }

    public override bool Input([NotNullWhen(true)] out string? text, bool lastOne = false)
    {
        _lastOne = lastOne;
        if (_seq.MoveNext()) //向下匹配一个参数
        {
            text = _seq.Current;
            return true;
        }
        text = null;
        return false;
    }
}
