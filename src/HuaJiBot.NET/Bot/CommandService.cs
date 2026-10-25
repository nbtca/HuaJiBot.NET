﻿using System.Text;
using System.Text.Json;
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
        (string description, Func<object?[]?, Task> method, PluginBase.CommandArgumentInfo[] info)
    > _commands = new();

    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    public string? ResolveAlias(string alias) => _aliases.GetValueOrDefault(alias);

    public IEnumerable<(string Alias, string Description)> AliasedCommands =>
        from x in _aliases
        select (x.Key, _commands[x.Value].description);

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
        if (!reader.Match(["help", "帮助"], x => x, out _, true))
            return false;
        SendHelp(e);
        return true;
    }

    public void SendHelp(GroupMessageEventArgs e)
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
                        continue;

                    string Quote(string str) => arg.IsOptional ? $"[{str}]" : $"<{str}>";
                    sb.Append(
                        arg.Attribute.ArgumentType switch
                        {
                            CommandArgumentType.String => Quote("string"),
                            CommandArgumentType.RegexString => Quote("regex"),
                            CommandArgumentType.Enum => Quote(
                                string.Join(
                                    "|",
                                    from x in ((CommandArgumentEnumAttributeBase)arg.Attribute).EnumItems
                                    select x.Key
                                )
                            ),
                            CommandArgumentType.Unknown => "unknown",
                            _ => throw new ArgumentOutOfRangeException(nameof(arg.Attribute.ArgumentType)),
                        }
                    );
                    sb.Append(' ');
                }
            }
            sb.AppendLine();
            sb.AppendLine($"    {description}");
        }
        var lines =
            from x in _commands
            let alias = _aliases.FirstOrDefault(a => a.Value == x.Key).Key
            let args = string.Concat(
                from arg in x.Value.info
                where arg.Attribute.ArgumentType != CommandArgumentType.Unknown
                select arg.IsOptional
                    ? $" [{arg.Attribute.Description}]"
                    : $" <{arg.Attribute.Description}>"
            )
            select $"• {(alias is null ? x.Key : "/" + alias)}{args} — {x.Value.description}";
        var post = new Post(
            "**可用命令**\n"
                + string.Join("\n", lines.Append("• /help — 查看全部命令").Select(Post.Escape)),
            sb.ToString()
        );
        _ = e.Service.SendGroupMessageAsync(e.RobotId, e.GroupId, new ReplyMessage(e.MessageId), post);
    }

    public void ProcessCommand(GroupMessageEventArgs e) => _ = ProcessCommandSafeAsync(e);

    public async Task<bool> ProcessCommandAsync(GroupMessageEventArgs e)
    {
        if (ProcessHelp(e))
            return true;

        var reader = e.CommandReader;
        if (!reader.Input(out var matched) || !_commands.TryGetValue(matched, out var matchedItem))
            return false;

        var (description, method, info) = matchedItem;
        _bot.LogDebug($"{description} : {e.TextMessage}");
        var args = ParseArguments(reader, info, e);
        await method(args);
        return true;
    }

    public async Task ExecuteCommandAsync(
        string commandId,
        JsonElement arguments,
        GroupMessageEventArgs e,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.Equals(commandId, "help", StringComparison.OrdinalIgnoreCase))
        {
            SendHelp(e);
            return;
        }

        var commandName = ResolveAlias(commandId);
        if (commandName is null
            && !_commands.ContainsKey(commandId))
            throw new ArgumentException($"未知命令：{commandId}", nameof(commandId));
        commandName ??= commandId;

        if (!_commands.TryGetValue(commandName, out var matchedItem))
            throw new ArgumentException($"当前未加载命令：{commandId}", nameof(commandId));

        var args = BuildArguments(matchedItem.info, arguments, e);
        await matchedItem.method(args);
    }

    public IReadOnlyList<CommandDescriptor> GetCommandCatalog()
    {
        var result = new List<CommandDescriptor>
        {
            new(
                "help",
                "帮助",
                "help",
                "查看全部命令",
                [],
                true,
                false,
                false,
                ["帮助", "有什么命令", "help"]
            )
        };

        foreach (var (name, (description, _, info)) in _commands)
        {
            var alias = _aliases.FirstOrDefault(x => x.Value == name).Key;
            var id = alias ?? name;
            var parameters = info
                .Select(arg => new CommandParameterDescriptor(
                    arg.Name,
                    arg.Attribute.Description,
                    arg.Attribute.ArgumentType,
                    arg.Type,
                    arg.IsOptional,
                    arg.Type == typeof(GroupMessageEventArgs)
                ))
                .ToArray();
            var policy = PolicyFor(id);
            result.Add(new CommandDescriptor(
                id,
                name,
                alias,
                description,
                parameters,
                policy.ReadOnly,
                policy.AdminOnly,
                policy.GroupScoped,
                ExamplesFor(id)
            ));
        }

        return result;
    }

    private async Task ProcessCommandSafeAsync(GroupMessageEventArgs e)
    {
        try
        {
            await ProcessCommandAsync(e);
        }
        catch (Exception exception)
        {
            _bot.LogError("执行命令时出现异常", exception);
        }
    }

    private static object?[]? ParseArguments(
        CommandReader reader,
        PluginBase.CommandArgumentInfo[] info,
        GroupMessageEventArgs e
    )
    {
        if (info.Length == 0)
            return null;

        var args = new object?[info.Length];
        for (var i = 0; i < info.Length; i++)
        {
            var arg = info[i];
            object? value = null;
            switch (arg.Attribute.ArgumentType)
            {
                case CommandArgumentType.Unknown when arg.Type == typeof(GroupMessageEventArgs):
                    value = e;
                    break;
                case CommandArgumentType.String:
                    if (!reader.Input(out var str))
                        continue;
                    value = str;
                    break;
                case CommandArgumentType.RegexString:
                {
                    var regexAttr = (CommandArgumentStringMatchAttribute)arg.Attribute;
                    if (!reader.Input(out var regexStr))
                        continue;
                    if (!Regex.IsMatch(regexStr, regexAttr.Pattern, regexAttr.Options))
                        continue;
                    value = regexStr;
                    break;
                }
                case CommandArgumentType.Enum:
                {
                    var enumAttr = (CommandArgumentEnumAttributeBase)arg.Attribute;
                    if (!reader.Match(enumAttr.EnumItems, x => [x.Key, x.Alias], out var item))
                        continue;
                    value = item.Value;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            args[i] = value;
        }
        return args;
    }

    private static object?[]? BuildArguments(
        PluginBase.CommandArgumentInfo[] info,
        JsonElement arguments,
        GroupMessageEventArgs e
    )
    {
        if (info.Length == 0)
            return null;

        var values = new object?[info.Length];
        for (var i = 0; i < info.Length; i++)
        {
            var arg = info[i];
            if (arg.Type == typeof(GroupMessageEventArgs))
            {
                values[i] = e;
                continue;
            }

            JsonElement valueElement = default;
            var hasValue = arguments.ValueKind == JsonValueKind.Object
                && arguments.TryGetProperty(arg.Name, out valueElement);
            if (!hasValue)
            {
                if (arg.IsOptional)
                {
                    values[i] = arg.DefaultValue;
                    continue;
                }
                throw new ArgumentException($"缺少参数：{arg.Name}", arg.Name);
            }

            values[i] = ConvertArgument(valueElement, arg);
        }
        return values;
    }

    private static object? ConvertArgument(JsonElement value, PluginBase.CommandArgumentInfo arg)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            if (arg.IsOptional)
                return arg.DefaultValue;
            throw new ArgumentException($"参数不能为空：{arg.Name}", arg.Name);
        }

        if (arg.Attribute is CommandArgumentEnumAttributeBase enumAttribute)
        {
            var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            text = NormalizeEnumValue(arg.Type, text);
            var enumItem = enumAttribute.EnumItems.FirstOrDefault(x =>
                string.Equals(x.Key, text, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Alias, text, StringComparison.OrdinalIgnoreCase)
            );
            if (enumItem is not null)
                return enumItem.Value;
            if (Enum.TryParse(arg.Type, text, true, out var enumValue))
                return enumValue;
            throw new ArgumentException($"参数 {arg.Name} 的值无效：{text}", arg.Name);
        }

        if (arg.Type == typeof(string))
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        if (arg.Type == typeof(bool))
            return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False
                ? value.GetBoolean()
                : bool.Parse(value.ToString());
        if (arg.Type == typeof(int))
            return value.GetInt32();
        if (arg.Type == typeof(long))
            return value.GetInt64();
        if (arg.Type == typeof(double))
            return value.GetDouble();
        if (arg.Type == typeof(decimal))
            return value.GetDecimal();
        if (arg.Type == typeof(DateTime))
            return value.GetDateTime();

        throw new ArgumentException($"不支持的参数类型：{arg.Type.Name}", arg.Name);
    }

    private static string NormalizeEnumValue(Type type, string? value)
    {
        var text = (value ?? "").Trim();
        if (type.Name == "SummaryTarget")
            return text.ToLowerInvariant() switch
            {
                "today" or "今天" or "今日" => "今天",
                "yesterday" or "昨天" or "昨日" => "昨天",
                "day_before_yesterday" or "before_yesterday" or "前天" => "前天",
                _ => text,
            };
        if (type.Name == "ClientEventType")
            return text.ToLowerInvariant() switch
            {
                "join" or "joinleft" or "加入" => "加入",
                "chat" or "聊天" => "聊天",
                "death" or "playerdeath" or "死亡" => "死亡",
                "achievement" or "progress" or "playerachievement" or "进度" => "进度",
                _ => text,
            };
        return text;
    }

    private static (bool ReadOnly, bool AdminOnly, bool GroupScoped) PolicyFor(string id) =>
        id.ToLowerInvariant() switch
        {
            "events" => (false, true, true),
            "tickets" or "summary" => (true, false, true),
            _ => (true, false, false),
        };

    private static string[] ExamplesFor(string id) =>
        id.ToLowerInvariant() switch
        {
            "schedule" => ["看看下周有什么安排", "最近两周的日程"],
            "next" => ["下一次活动是什么", "最近有什么日程"],
            "summary" => ["今天群里聊了什么", "总结一下昨天"],
            "mc" => ["服务器现在有人吗", "看看 MC 在线情况"],
            "events" => ["把死亡事件关掉", "打开聊天事件转发"],
            "tickets" => ["还有维修工单吗", "看看进行中的维修单"],
            _ => [],
        };

    public void SetupCommands(PluginBase plugin)
    {
        foreach (var (name, description, method, info, alias) in plugin.GetAllCommands())
        {
            _commands.Add(name, (description, method, info));
            if (alias is not null)
                _aliases.Add(alias, name);
            _bot.Log($"读取命令 {name} ，描述：{description}");
        }
        _bot.OnCommandsRegistered(AliasedCommands.Append(("help", "查看全部命令")));
        if (_commands.Count != 0)
        {
            void ProcessCommandInternal(object? sender, GroupMessageEventArgs e) => ProcessCommand(e);
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