using System;

namespace HuaJiBot.NET.Events;

/// <summary>
/// 机器人账号登录成功事件
/// </summary>
public class BotLoginEventArgs : EventArgs
{
    /// <summary>账户ID</summary>
    public required string AccountId { get; init; }

    public required string ClientName { get; init; }
    public required string ClientVersion { get; init; }
}

/// <summary>
/// 群消息事件
/// </summary>
public class GroupMessageEventArgs : EventArgs
{
    /// <summary>群ID</summary>
    public required string GroupId { get; init; }

    /// <summary>群名称</summary>
    public required string GroupName { get; init; }

    /// <summary>发送者ID</summary>
    public required string SenderId { get; init; }

    /// <summary>发送者群名片</summary>
    public required string SenderMemberCard { get; init; }
    public required Lazy<string> TextMessageLazy { get; init; }
    public string TextMessage => TextMessageLazy.Value;
}
