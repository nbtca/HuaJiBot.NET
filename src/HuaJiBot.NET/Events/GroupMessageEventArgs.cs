using System;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;

namespace HuaJiBot.NET.Events;

public class BotEventArgs : EventArgs
{
    public required BotServiceBase Service { get; init; }
}

/// <summary>
/// 机器人账号登录成功事件
/// </summary>
public class BotLoginEventArgs : BotEventArgs
{
    /// <summary>账户ID</summary>
    public required string[] Accounts { get; init; }

    public required string ClientName { get; init; }
    public required string? ClientVersion { get; init; }
}

/// <summary>
/// 群消息事件
/// </summary>
public class GroupMessageEventArgs(
    Func<CommandReader> createCommandReader,
    Func<ValueTask<string>> getGroupName
) : BotEventArgs
{
    public string? RobotId { get; init; }

    /// <summary>群ID</summary>
    public required string GroupId { get; init; }

    /// <summary>发送者ID</summary>
    public required string SenderId { get; init; }

    /// <summary>发送者群名片</summary>
    public required string SenderMemberCard { get; init; }
    public required Lazy<string> TextMessageLazy { get; init; }
    public string TextMessage => TextMessageLazy.Value;
    public CommandReader CommandReader => createCommandReader();

    /// <summary>
    /// 返回消息给发送者（at发送者）
    /// </summary>
    /// <param name="message"></param>
    public void Feedback(string message)
    {
        Service.FeedbackAt(RobotId, GroupId, SenderId, message);
    }

    /// <summary>群名称</summary>
    public ValueTask<string> GetGroupNameAsync() => getGroupName();
}
