using System.Text;
using System.Text.RegularExpressions;
using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.Bot;

public class CommandService : ICommandService
{
    private readonly BotService _bot;

    readonly Dictionary<
        string,
        (string description, Action<object?[]?> method, PluginBase.CommandArgumentInfo[] info)
    > _commands = new();

    private readonly Dictionary<string, IEnumerable<AgentFunctionInfo>> _exportFunctions = new();

    public IReadOnlyDictionary<string, IEnumerable<AgentFunctionInfo>> ExportFunctions =>
        _exportFunctions;

    public CommandService(BotService bot)
    {
        _bot = bot;
    }

    public bool ProcessHelp(GroupMessageEventArgs e)
    {
        var reader = e.CommandReader;
        if (reader.Match(["help", "帮助"], x => x, out _, true))
        {
            var sb = new StringBuilder();
            sb.AppendLine("可用命令：");
            foreach (var (name, (description, _, info)) in _commands)
            {
                sb.Append(name);
                if (info.Length != 0)
                {
                    sb.Append(" ");
                    foreach (var arg in info)
                    {
                        if (arg.Attribute.ArgumentType == CommandArgumentType.Unknown)
                        {
                            continue;
                        }

                        string Quote(string str)
                        {
                            if (arg.IsOptional)
                                return $"[{str}]";
                            return $"<{str}>";
                        }
                        sb.Append(
                            arg.Attribute.ArgumentType switch
                            {
                                CommandArgumentType.String => Quote("string"),
                                CommandArgumentType.RegexString => Quote("regex"),
                                CommandArgumentType.Enum => Quote(
                                    string.Join(
                                        "|",
                                        from x in (
                                            (CommandArgumentEnumAttributeBase)arg.Attribute
                                        ).EnumItems
                                        select x.Key
                                    )
                                ),
                                CommandArgumentType.Unknown => "unknown",
                                _ => throw new ArgumentOutOfRangeException(
                                    nameof(arg.Attribute.ArgumentType)
                                ),
                            }
                        );
                        sb.Append(' ');
                    }
                }
                sb.AppendLine();
                sb.AppendLine($"    {description}");
            }
            e.Reply(sb.ToString());
            return true;
        }
        return false;
    }

    public void ProcessCommand(GroupMessageEventArgs e)
    {
        if (ProcessHelp(e))
        {
            return;
        }
        var reader = e.CommandReader;
        if (
            reader.Match(_commands.Keys, out var matched)
            && _commands.TryGetValue(matched, out var matchedItem)
        )
        {
            var (description, method, info) = matchedItem;
            _bot.LogDebug($"{description} : {e.TextMessage}");
            object?[]? args = null;
            if (info.Any())
            {
                args = new object?[info.Length];
                for (var i = 0; i < info.Length; i++)
                {
                    var arg = info[i];
                    object? value = null;
                    switch (arg.Attribute.ArgumentType)
                    {
                        case CommandArgumentType.Unknown
                            when arg.Type == typeof(GroupMessageEventArgs):
                            value = e;
                            break;
                        case CommandArgumentType.String:
                            {
                                if (!reader.Input(out var str))
                                    continue;
                                value = str;
                            }
                            break;
                        case CommandArgumentType.RegexString:
                            {
                                var regexAttr = (CommandArgumentStringMatchAttribute)arg.Attribute;
                                if (!reader.Input(out var str))
                                    continue;
                                if (!Regex.IsMatch(str, regexAttr.Pattern, regexAttr.Options))
                                    continue;
                                value = str;
                            }
                            break;
                        case CommandArgumentType.Enum:
                            {
                                var enumAttr = (CommandArgumentEnumAttributeBase)arg.Attribute;
                                if (
                                    !reader.Match(
                                        enumAttr.EnumItems,
                                        x => [x.Key, x.Alias], //枚举的key或者别名，匹配到同一个元素
                                        out var item
                                    )
                                )
                                {
                                    continue;
                                }
                                value = item.Value;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    args[i] = value;
                }
            }
            //匹配参数
            method(args);
        }
    }

    public void SetupCommands(PluginBase plugin)
    {
        foreach (var (name, description, method, info) in plugin.GetAllCommands())
        {
            _commands.Add(name, (description, method, info));
            _bot.Log($"读取命令 {name} ，描述：{description}");
        }
        if (_commands.Count != 0)
        {
            //监听群消息事件，匹配命令
            void ProcessCommandInternal(object? sender, GroupMessageEventArgs e)
            {
                ProcessCommand(e);
            }
            _bot.Events.OnGroupMessageReceived -= ProcessCommandInternal;
            _bot.Events.OnGroupMessageReceived += ProcessCommandInternal;
            _bot.Log($"从插件 {plugin.Name} 加载了 {_commands.Count} 条命令");
        }
        if (plugin.ExportFunctions is { } exportFunctions)
        {
            var pluginName = plugin.Name;
            _exportFunctions[pluginName] = exportFunctions;
        }
    }
}
