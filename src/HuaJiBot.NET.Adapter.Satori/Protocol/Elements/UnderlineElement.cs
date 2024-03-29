﻿namespace HuaJiBot.NET.Adapter.Satori.Protocol.Elements;

/// <summary>
/// 下划线
/// </summary>
public class UnderlineElement : Element
{
    public override string TagName => "u";

    public override string[] AlternativeTagNames => ["ins"];
}
