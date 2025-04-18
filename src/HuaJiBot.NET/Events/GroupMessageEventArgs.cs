﻿using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;

namespace HuaJiBot.NET.Events;

public class BotEventArgs : EventArgs
{
    public required BotService Service { get; init; }
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

    /// <summary>
    /// 消息ID
    /// </summary>
    public required string MessageId { get; init; }

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
    public async Task<string[]> Reply(string message)
    {
        return await Service.FeedbackAt(RobotId, GroupId, MessageId, message);
    }

    /// <summary>群名称</summary>
    public ValueTask<string> GetGroupNameAsync() => getGroupName();
}

/// <summary>
/// 私聊消息事件
/// </summary>
public class PrivateMessageEventArgs(Func<CommandReader> createCommandReader) : BotEventArgs
{
    public string? RobotId { get; init; }

    /// <summary>
    /// 消息ID
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>群ID</summary>
    public required string? GroupId { get; init; }

    /// <summary>发送者ID</summary>
    public required string SenderId { get; init; }

    public required Lazy<string> TextMessageLazy { get; init; }
    public string TextMessage => TextMessageLazy.Value;
    public CommandReader CommandReader => createCommandReader();

    /// <summary>
    /// 返回消息给发送者（at发送者）
    /// </summary>
    /// <param name="message"></param>
    public void Reply(string message)
    {
        //Service.FeedbackAt(RobotId, GroupId, MessageId, message);
    }
}
