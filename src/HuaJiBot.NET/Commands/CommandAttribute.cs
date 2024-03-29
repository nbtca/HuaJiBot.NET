using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HuaJiBot.NET.Commands;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string key, string description) : Attribute
{
    public string Key => key;
    public string Description => description;
}

public enum CommandArgumentType
{
    Unknown,
    Enum,
    String,
    RegexString
}

[AttributeUsage(AttributeTargets.Parameter)]
public abstract class CommandArgumentAttribute(string description) : Attribute
{
    public string Description => description;
    public abstract CommandArgumentType ArgumentType { get; }
}

public abstract class CommandArgumentEnumAttributeBase(string description)
    : CommandArgumentAttribute(description)
{
    public override CommandArgumentType ArgumentType => CommandArgumentType.Enum;
    public abstract bool TryParse(string input, [NotNullWhen(true)] out object? value);
    private EnumInfo[] _enumItems;
    public EnumInfo[] EnumItems => _enumItems ??= AllEnumItems().ToArray();

    public record EnumInfo(string Key, string Alias, string Description, object Value);

    protected abstract IEnumerable<EnumInfo> AllEnumItems();
}

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentEnumAttribute<T>(string description)
    : CommandArgumentEnumAttributeBase(description)
    where T : struct, Enum
{
    public override CommandArgumentType ArgumentType => CommandArgumentType.Enum;

    public override bool TryParse(string input, [NotNullWhen(true)] out object? value)
    {
        if (Enum.TryParse<T>(input, out var result))
        {
            value = result;
            return true;
        }
        value = null;
        return false;
    }

    protected override IEnumerable<EnumInfo> AllEnumItems()
    {
        foreach (var value in Enum.GetValues<T>())
        {
            var key = Enum.GetName(value)!;
            var field = typeof(T).GetField(key);
            var attr = field?.GetCustomAttribute<CommandEnumItemAttribute>();
            var description = attr?.Description ?? key;
            var alias = attr?.Alias ?? key;
            yield return new EnumInfo(key, alias, description, value);
        }
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentStringAttribute(string description)
    : CommandArgumentAttribute(description)
{
    public override CommandArgumentType ArgumentType => CommandArgumentType.String;
}

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentUnknownAttribute(Type type) : CommandArgumentAttribute(type.Name)
{
    public override CommandArgumentType ArgumentType => CommandArgumentType.Unknown;
    public Type Type => type;
}

[AttributeUsage(AttributeTargets.Parameter)]
public class CommandArgumentStringMatchAttribute(
    [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern,
    RegexOptions options,
    string description
) : CommandArgumentAttribute(description)
{
    public string Pattern => pattern;
    public RegexOptions Options => options;
    public override CommandArgumentType ArgumentType => CommandArgumentType.RegexString;
}

[AttributeUsage(AttributeTargets.Enum)]
public class CommandEnumAttribute(string description) : Attribute
{
    public string Description => description;
}

[AttributeUsage(AttributeTargets.Field)]
public class CommandEnumItemAttribute(string alias, string description) : Attribute
{
    public string Description => description;
    public string Alias => alias;
}
