using System.Text;
using System.Text.RegularExpressions;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Bot;

public enum MemberType
{
    Unknown = 0,
    Member = 1,
    Admin = 2,
    Owner = 3
}

public abstract record SendingMessageBase
{
    public static implicit operator SendingMessageBase(string text) => new TextMessage(text);
};

public sealed record TextMessage(string Text) : SendingMessageBase;

public sealed record ImageMessage(string ImagePath) : SendingMessageBase;

public sealed record AtMessage(string Target) : SendingMessageBase;

public sealed record ReplyMessage(string? ReplayMsgSeq, string ReplyMsgId, string? Target)
    : SendingMessageBase;

public abstract class BotServiceBase
{
    #region Logger
    public abstract ILogger Logger { get; init; }

    public void Log(object message) => Logger.Log(message);

    public void Warn(object message) => Logger.Warn(message);

    public void LogDebug(object message) => Logger.LogDebug(message);

    public void LogError(object message, object detail) => Logger.LogError(message, detail);
    #endregion
    public abstract void Reconnect();
    public abstract Task SetupServiceAsync();
    public Config.ConfigWrapper Config { get; internal set; } = null!;
    public Events.Events Events { get; } = new();
    public abstract string[] AllRobots { get; }
    public abstract void SendGroupMessage(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    );

    public virtual void FeedbackAt(string? robotId, string targetGroup, string userId, string text)
    {
        SendGroupMessage(robotId, targetGroup, new AtMessage(userId), new TextMessage(text));
    }

    public abstract MemberType GetMemberType(string robotId, string targetGroup, string userId);
    public abstract string GetNick(string robotId, string userId);

    public abstract string GetPluginDataPath();

    private bool ProcessHelp(GroupMessageEventArgs e)
    {
        var reader = e.CommandReader;
        if (reader.Match(["help", "帮助"], x => x, out _))
        {
            var sb = new StringBuilder();
            sb.AppendLine("可用命令：");
            foreach (var (name, (description, _, info)) in _commands)
            {
                sb.Append(name);
                if (info.Any())
                {
                    sb.Append(" ");
                    foreach (var arg in info)
                    {
                        if (arg.Attribute.ArgumentType == CommandArgumentType.Unknown)
                        {
                            continue;
                        }
                        sb.Append(
                            arg.Attribute.ArgumentType switch
                            {
                                CommandArgumentType.String => "<string>",
                                CommandArgumentType.RegexString => "<regex>",
                                CommandArgumentType.Enum => "<enum>",
                            }
                        );
                        if (!arg.IsOptional)
                        {
                            sb.Append('*');
                        }
                        sb.Append(' ');
                    }
                }
                sb.AppendLine();
                sb.AppendLine($"    {description}");
            }
            e.Feedback(sb.ToString());
            return true;
        }
        return false;
    }

    private void ProcessCommand(GroupMessageEventArgs e)
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
                                        x => [x.Key, x.Alias],
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

    readonly Dictionary<
        string,
        (string description, Action<object?[]?> method, PluginBase.CommandArgumentInfo[] info)
    > _commands = new();

    public void LoadAddCommand(PluginBase plugin)
    {
        foreach (var (name, description, method, info) in plugin.GetAllCommands())
        {
            _commands.Add(name, (description, method, info));
            Log($"读取命令 {name} ，描述：{description}");
        }
        if (_commands.Any())
        {
            //监听群消息事件，匹配命令
            Events.OnGroupMessageReceived += (sender, e) => ProcessCommand(e);
            Log($"从插件 {plugin.Name} 加载了 {_commands.Count} 条命令");
        }
    }
}
