using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.MessageBridge.Types;
using HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;
using HuaJiBot.NET.Websocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ClientEventType = HuaJiBot.NET.Plugin.MessageBridge.PluginConfig.GroupConfig.ClientEventType;

namespace HuaJiBot.NET.Plugin.MessageBridge;

public class PluginConfig : ConfigBase
{
    public ClientInfo[] Clients { get; set; } = [];

    public class ClientInfo
    {
        public ClientType Type { get; set; } = ClientType.Minecraft;
        public string Address { get; set; } = "ws://localhost:8080";
        public string Token { get; set; } = "token";
        public GroupConfig[] Groups { get; set; } = [];
    }

    public class GroupConfig
    {
        public string GroupId { get; set; } = "123456";
        public bool Enabled { get; set; } = true;

        /**
         *  是否将来自群的消息转发给客户端
         */
        public bool ForwardToClient { get; set; } = true;

        /**
         *  是否将来自客户端的消息转发给群
         */
        public bool ForwardFromClient { get; set; } = true;
        public HashSet<ClientEventType> ForwardFromClientDisabledEvent { get; set; } = [];

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ClientEventType
        {
            [CommandEnumItem("加入", "玩家进退服务器")]
            JoinLeft,

            [CommandEnumItem("聊天", "玩家在聊天框发送消息")]
            Chat,

            [CommandEnumItem("死亡", "玩家死亡")]
            PlayerDeath,

            [CommandEnumItem("进度", "玩家获得成就")]
            PlayerAchievement,
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClientType
    {
        Minecraft,
    }
}

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();
    private readonly Dictionary<PluginConfig.ClientInfo, IServerlessMQ> _clients = new();

    //初始化
    protected override async Task InitializeAsync()
    {
        foreach (var clientInfo in Config.Clients)
        {
            // 构建自定义请求头
            var headers = new Dictionary<string, string>
            {
                ["client-type"] = "IM",
                ["client-subtype"] = "QQ",
                ["client-name"] = BasePacket.DefaultInformation?.Name ?? "Unknown",
                ["client-version"] = BasePacket.DefaultInformation?.Version ?? "Unknown",
                ["address"] = clientInfo.Address,
            };

            // 创建 ILogger
            ILogger logger = Service.Logger;

            var client = new ServerlessMQ(
                url: clientInfo.Address,
                token: clientInfo.Token,
                logger: logger,
                headers: headers
            );

            // 订阅连接事件
            client.OnConnected += info =>
            {
                var status = info.IsReconnect ? "重新连接" : "连接";
                Info($"[{clientInfo.Address}] {status}成功 - {info.Timestamp:HH:mm:ss}");
            };

            client.OnClosed += info =>
            {
                Info(
                    $"[{clientInfo.Address}] 连接断开 - 类型: {info.Type}, 原因: {info.Reason ?? "未知"}"
                );
            };

            // 订阅消息接收事件
            client.OnPacket += async data =>
            {
                try
                {
                    var messageRaw = data.ToString();
                    await ProcessMessageFromClientAsync(messageRaw, clientInfo);
                }
                catch (Exception e)
                {
                    Error($"[{clientInfo.Address}] 处理消息时出现异常：", e);
                }
            };

            _clients.Add(clientInfo, client);
        }

        Service.Events.OnGroupMessageReceived += (s, e) => _ = ProcessMessageFromGroupAsync(e);
        Service.Events.OnBotLogin += (_, e) =>
        {
            BasePacket.DefaultInformation = new SenderInformation(
                "QQGroup",
                $"HuaJiBot.NET.Plugin.MessageBridge({e.ClientName})",
                e.ClientVersion ?? "?"
            );
        };
        Info("启动成功！");
    }

    private async Task ProcessMessageFromGroupAsync(GroupMessageEventArgs e)
    {
        var groupName = await e.GetGroupNameAsync();
        List<Action<string>> sendActions = [];
        var groupId = e.GroupId.Split(":")[0];
        foreach (var clientInfo in Config.Clients)
        {
            if (
                clientInfo
                    .Groups.Where(x => x is { Enabled: true, ForwardToClient: true })
                    .Any(x => x.GroupId.Split(":")[0] == groupId)
            )
            {
                if (_clients.TryGetValue(clientInfo, out var client))
                    sendActions.Add(msg => client.Send(msg));
            }
        }

        if (sendActions.Any())
        {
            var pkt = new GroupMessagePacket
            {
                Data = new GroupMessagePacketData
                {
                    SenderName = e.SenderMemberCard,
                    GroupName = groupName,
                    SenderId = e.SenderId,
                    GroupId = e.GroupId,
                    Message =
                        e.TextMessage //todo structure message
                    ,
                },
                Source = BasePacket.DefaultInformation,
            };
            var str = pkt.ToJson();
            foreach (var action in sendActions)
                action(str);
        }
    }

    private readonly ConcurrentDictionary<
        string,
        (string groupId, string[] msgId)[]
    > _onlinePlayers = new();

    private async ValueTask ProcessMessageFromClientAsync(
        string messageRaw,
        PluginConfig.ClientInfo clientInfo
    )
    {
        try
        {
            var message = BasePacket.FromJson(messageRaw);
            var senderName = message?.Source?.DisplayName ?? "Unknown";
            switch (clientInfo.Type)
            {
                case PluginConfig.ClientType.Minecraft:
                    switch (message)
                    {
                        case PlayerChatPacket { Data: { Message: var msg, PlayerName: var name } }:
                            _ = SendGroupMessageAsync(
                                clientInfo,
                                ClientEventType.Chat,
                                $"[{senderName}] <{name}> {msg}"
                            );
                            break;
                        case PlayerJoinPacket { Data.PlayerName: var name }:
                            _onlinePlayers[name] = await SendGroupMessageAsync(
                                clientInfo,
                                ClientEventType.JoinLeft,
                                $"[{senderName}] {name} 加入了服务器"
                            );
                            break;
                        case PlayerQuitPacket { Data.PlayerName: var name }:
                            _ = SendGroupMessageAsync(
                                clientInfo,
                                ClientEventType.JoinLeft,
                                $"[{senderName}] {name} 离开了服务器"
                            );
                            if (_onlinePlayers.TryRemove(name, out var allMsg))
                            {
                                foreach (var (groupId, msgIds) in allMsg)
                                {
                                    foreach (var msgId in msgIds)
                                    {
                                        try
                                        {
                                            Service.RecallMessage(null, groupId, msgId);
                                        }
                                        catch (Exception e)
                                        {
                                            Warn(
                                                $"撤回消息失败(groupId={groupId}, msgId={msgId})：",
                                                e
                                            );
                                        }
                                    }
                                }
                            }
                            break;
                        case PlayerDeathPacket { Data.DeathMessage: var msg }:
                            _ = SendGroupMessageAsync(
                                clientInfo,
                                ClientEventType.PlayerDeath,
                                $"[{senderName}] {msg}"
                            );
                            break;
                        case PlayerAchievementPacket
                        {
                            Data:
                            {
                                PlayerName: var name,
                                Name: var achievementName,
                                Criteria: var criteria,
                                Description: var description
                            }
                        }:
                            _ = SendGroupMessageAsync(
                                clientInfo,
                                ClientEventType.PlayerAchievement,
                                $"[{senderName}] {name} 完成了进度 {achievementName} ({string.Join(",", criteria)}){Environment.NewLine}{description}"
                            );
                            break;
                        case GetPlayerListRequestPacket: //do not reply
                            break;
                        case GetPlayerListResponsePacket playerListResponse:
                            ProcessPlayerListResponse(playerListResponse);
                            break;
                        case ActiveBroadcastPacket activeBroadcast:
                            ProcessActiveBroadcast(activeBroadcast);
                            break;
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Error("处理消息时出现异常：" + messageRaw, e);
        }
    }

    private async Task<(string groupId, string[] msgId)[]> SendGroupMessageAsync(
        PluginConfig.ClientInfo clientInfo,
        ClientEventType eventType,
        string message
    )
    {
        List<(string, string[])> msgIds = [];
        foreach (
            var config in from config in clientInfo.Groups
            where config is { Enabled: true, ForwardFromClient: true }
            where !config.ForwardFromClientDisabledEvent.Contains(eventType)
            select config
        )
        {
            msgIds.Add(
                (
                    config.GroupId,
                    await Service.SendGroupMessageAsync(
                        null,
                        config.GroupId,
                        new TextMessage(message)
                    )
                )
            );
        }
        return msgIds.ToArray();
    }

    private static bool StringToToggle(string input, out bool result)
    {
        switch (input.ToLower())
        {
            case "开" or "开启" or "on" or "true":
                result = true;
                return true;
            case "关" or "关闭" or "off" or "false":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    public string EnumToAttributeName<T>(T value)
        where T : Enum
    {
        Type enumType = typeof(T);
        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null)?.Equals(value) ?? false)
            {
                return field.GetCustomAttribute<CommandEnumItemAttribute>()?.Alias ?? field.Name;
            }
        }
        return value.ToString();
    }

    [Command("事件", "开关事件")]
    // ReSharper disable once UnusedMember.Global
    public void EventControlCommand(
        [CommandArgumentEnum<ClientEventType>("类型")] ClientEventType? typeOptional,
        [CommandArgumentString("状态")] string? status,
        GroupMessageEventArgs e
    )
    {
        if (typeOptional is not { } type)
        {
            var allStatus = new StringBuilder();
            foreach (var clientInfo in Config.Clients)
            {
                if (
                    clientInfo.Groups.FirstOrDefault(x => x.GroupId == e.GroupId) is
                    { Enabled: true } group
                )
                {
                    allStatus.AppendLine($"[{group.GroupId}]");
                    foreach (var eventType in Enum.GetValues<ClientEventType>())
                    {
                        var enumName = EnumToAttributeName(eventType);
                        var disabled = group.ForwardFromClientDisabledEvent.Contains(eventType);
                        allStatus.AppendLine($"    {enumName}：{(disabled ? "关闭" : "开启")}");
                    }
                }
            }
            e.Reply(
                "类型 可选："
                    + string.Join(
                        ", ",
                        from x in Enum.GetValues<ClientEventType>()
                        select EnumToAttributeName(x)
                    )
                    + $"\n当前状态：\n{allStatus}"
            );
            return;
        }
        var name = EnumToAttributeName(type);
        if (!StringToToggle(status ?? "", out var result))
        {
            var currentStatusDisabled =
                Config
                    .Clients.SelectMany(x => x.Groups)
                    .FirstOrDefault(x => x is { Enabled: true })
                    ?.ForwardFromClientDisabledEvent.Contains(type)
                ?? true;
            e.Reply("状态 可选：true、false\n" + $"当前{name}状态：{!currentStatusDisabled}");
            return;
        }
        foreach (var clientInfo in Config.Clients)
        { //TODO：多client
            if (
                clientInfo.Groups.FirstOrDefault(x => x.GroupId == e.GroupId) is
                { Enabled: true } group
            )
            {
                if (result)
                    group.ForwardFromClientDisabledEvent.Remove(type);
                else
                    group.ForwardFromClientDisabledEvent.Add(type);
                Service.Config.Save();
                e.Reply($"已 {(result ? "开启" : "关闭")} 事件 {name} 的转发");
                return;
            }
        }
        e.Reply($"未找到群 {e.GroupId} 的配置");
    }

    protected override void Unload()
    {
        // 释放所有 WebSocket 连接
        foreach (var (_, client) in _clients)
        {
            try
            {
                client.Dispose();
            }
            catch (Exception e)
            {
                Warn("释放 WebSocket 连接时出现异常：", e);
            }
        }
        _clients.Clear();
    }
}
