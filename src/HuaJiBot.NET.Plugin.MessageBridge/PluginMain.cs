using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Plugin.MessageBridge.Types;
using HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Websocket.Client;
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
        public HashSet<ClientEventType> ForwardFromClientDisabledEvent { get; set; } = new();

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
            PlayerAchievement
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClientType
    {
        Minecraft
    }
}

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();
    private readonly Dictionary<PluginConfig.ClientInfo, WebsocketClient> _clients = new();

    //初始化
    protected override async Task InitializeAsync()
    {
        foreach (var clientInfo in Config.Clients)
        {
            WebsocketClient client =
                new(
                    new Uri(clientInfo.Address),
                    () =>
                    {
                        var cfg = new ClientWebSocket
                        {
                            Options =
                            {
                                KeepAliveInterval = TimeSpan.FromSeconds(5),
                                CollectHttpResponseDetails = true
                            }
                        };
                        if (!string.IsNullOrEmpty(clientInfo.Token))
                        {
                            cfg.Options.SetRequestHeader(
                                "Authorization",
                                "Bearer " + clientInfo.Token
                            );
                            cfg.Options.SetRequestHeader("client-type", "IM");
                            cfg.Options.SetRequestHeader("client-subtype", "QQ");
                            cfg.Options.SetRequestHeader(
                                "client-name",
                                BasePacket.DefaultInformation?.Name
                            );
                            cfg.Options.SetRequestHeader(
                                "client-version",
                                BasePacket.DefaultInformation?.Version
                            );
                            cfg.Options.SetRequestHeader("address", clientInfo.Address);
                        }
                        return cfg;
                    }
                )
                {
                    IsReconnectionEnabled = true,
                    ReconnectTimeout = null,
                    MessageEncoding = Encoding.UTF8,
                    IsTextMessageConversionEnabled = true
                };
            client.MessageReceived.Subscribe(msg =>
            {
                if (msg.MessageType == WebSocketMessageType.Text)
                {
                    try
                    {
                        ProcessMessageFromClient(
                            msg.Text ?? throw new NullReferenceException("msg.Text"),
                            clientInfo
                        );
                    }
                    catch (Exception e)
                    {
                        Error("处理消息时出现异常：", e);
                    }
                }
                else
                {
                    Info("收到非文本消息！");
                }
            });
            client.DisconnectionHappened.Subscribe(info =>
                Info("Disconnection Happened " + info.Type)
            );
            client.ReconnectionHappened.Subscribe(info =>
                Info("Reconnection Happened " + info.Type)
            );
            await client.Start();
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
        List<Action<string>> sendActions = new();
        foreach (var clientInfo in Config.Clients)
        {
            if (
                clientInfo
                    .Groups.Where(x => x is { Enabled: true, ForwardToClient: true })
                    .Any(x => x.GroupId == e.GroupId)
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
                    Message = e.TextMessage //todo structure message
                },
                Source = BasePacket.DefaultInformation
            };
            var str = pkt.ToJson();
            foreach (var action in sendActions)
                action(str);
        }
    }

    private void ProcessMessageFromClient(string messageRaw, PluginConfig.ClientInfo clientInfo)
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
                            SendGroupMessage(
                                clientInfo,
                                ClientEventType.Chat,
                                $"[{senderName}] <{name}> {msg}"
                            );
                            break;
                        case PlayerJoinPacket { Data.PlayerName: var name }:
                            SendGroupMessage(
                                clientInfo,
                                ClientEventType.JoinLeft,
                                $"[{senderName}] {name} 加入了服务器"
                            );
                            break;
                        case PlayerQuitPacket { Data.PlayerName: var name }:
                            SendGroupMessage(
                                clientInfo,
                                ClientEventType.JoinLeft,
                                $"[{senderName}] {name} 离开了服务器"
                            );
                            break;
                        case PlayerDeathPacket { Data.DeathMessage: var msg }:
                            SendGroupMessage(
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
                            SendGroupMessage(
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

    private void SendGroupMessage(
        PluginConfig.ClientInfo clientInfo,
        ClientEventType eventType,
        string message
    )
    {
        foreach (
            var config in from config in clientInfo.Groups
            where config is { Enabled: true, ForwardFromClient: true }
            where !config.ForwardFromClientDisabledEvent.Contains(eventType)
            select config
        )
        {
            Service.SendGroupMessage(null, config.GroupId, new TextMessage(message));
        }
    }

    private bool stringToToggle(string input, out bool result)
    {
        switch (input.ToLower())
        {
            case "开"
            or "开启"
            or "on"
            or "true":
                result = true;
                return true;
            case "关"
            or "关闭"
            or "off"
            or "false":
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
        if (!stringToToggle(status ?? "", out var result))
        {
            var currentStatusDisabled =
                Config
                    .Clients.SelectMany(x => x.Groups)
                    .FirstOrDefault(x => x is { Enabled: true })
                    ?.ForwardFromClientDisabledEvent.Contains(type) ?? true;
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

    protected override void Unload() { }
}
