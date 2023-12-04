using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace HuaJiBot.NET.Commands;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string description) : Attribute { }

[AttributeUsage(AttributeTargets.Parameter)]
public abstract class CommandArgumentAttribute(string description) : Attribute
{
    internal string Description => description;
}

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentEnumAttribute<T>(string description)
    : CommandArgumentAttribute(description)
    where T : Enum;

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentStringAttribute(string description)
    : CommandArgumentAttribute(description);

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentStringMatchAttribute(
    [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern,
    RegexOptions options,
    string description
) : CommandArgumentAttribute(description);

[AttributeUsage(AttributeTargets.Enum)]
public class CommandEnumAttribute(string description) : CommandArgumentAttribute(description);

[AttributeUsage(AttributeTargets.Field)]
public class CommandEnumItemAttribute(string description) : CommandArgumentAttribute(description);
