using System;
using System.Diagnostics.CodeAnalysis;

namespace HuaJiBot.NET.Commands;

public abstract class CommandReader
{
    public abstract bool Match(string[] expected, [NotNullWhen(true)] out string? matched);
    public abstract bool Input([NotNullWhen(true)] out string? text);
    //public abstract bool At(out string id);
}
