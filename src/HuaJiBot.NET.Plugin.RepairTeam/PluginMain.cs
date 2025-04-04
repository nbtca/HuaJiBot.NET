﻿using System.Text;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HuaJiBot.NET.Plugin.RepairTeam;

public class PluginConfig : ConfigBase
{
    public string NsqUrl = "";
    public string NsqSecret = "";
    public string NsqTopic = "";
    public string NsqChannel = "";
    public string? PushRawGroup = "";
    public string[] PushInfoGroup = [];
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    private NsqConnector? _nsq;

    protected override void Initialize()
    {
        _nsq = new(Config.NsqUrl, Config.NsqTopic, Config.NsqChannel, Config.NsqSecret);
        Service.Log("[RepairTem] 启动成功！");
        _nsq.MessageReceived += OnMessageReceived;
    }

    private enum ActionType
    {
        // ReSharper disable InconsistentNaming
        create = 1,
        accept = 2,
        cancel = 3,
        commit = 4,
        alterCommit = 5,
        drop = 6,
        close = 7,
        reject = 8,
        update = 9,
        // ReSharper restore InconsistentNaming
    }

    private class LogEventEntity
    {
        [JsonProperty("action")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("event_id")]
        public long EventId { get; set; }

        [JsonProperty("gmt_create")]
        public DateTimeOffset GmtCreate { get; set; }

        [JsonProperty("member_alias")]
        public string? MemberAlias { get; set; }

        [JsonProperty("member_id")]
        public string? MemberId { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("problem")]
        public string? Problem { get; set; }
    }

    private void OnMessageReceived(object? sender, string msg)
    {
        Service.Log(msg);
        if (!string.IsNullOrWhiteSpace(Config.PushRawGroup))
            Service.SendGroupMessageAsync(null, Config.PushRawGroup, msg);
        if (Config.PushInfoGroup.Length != 0)
        {
            var e = JsonConvert.DeserializeObject<LogEventEntity>(msg)!;
            var s = new StringBuilder();
            s.AppendLine($"---维修事件---");
            s.AppendLine($"ID：{e.EventId}");
            s.AppendLine($"类型：{e.Action}");
            s.AppendLine($"时间：{e.GmtCreate.ToString("f")}");
            if (!string.IsNullOrWhiteSpace(e.MemberId))
                s.AppendLine($"人员：{e.MemberId}({e.MemberAlias})");
            if (!string.IsNullOrWhiteSpace(e.Model))
                s.AppendLine($"机型：{e.Model}");
            if (!string.IsNullOrWhiteSpace(e.Problem))
                s.AppendLine($"问题：{e.Problem}");
            if (!string.IsNullOrWhiteSpace(e.Description))
                s.AppendLine($"描述：{e.Description}");
            var str = s.ToString();
            Task.Run(() =>
            {
                foreach (var group in Config.PushInfoGroup)
                {
                    Service.SendGroupMessageAsync(null, group, str);
                }
            });
        }
    }

    //[CommandEnum("test")]
    //enum TEST
    //{
    //    [CommandEnumItem("ss", "")]
    //    s,
    //    [CommandEnumItem("aa", "")]
    //    a
    //}
    [Command("test", "")]
    // ReSharper disable once UnusedMember.Global
    public void TestCommand(
        [CommandArgumentString("test")] string? a,
        GroupMessageEventArgs e
    //[CommandArgumentEnum<TEST>("test")] TEST b
    )
    {
        if (!string.IsNullOrWhiteSpace(a))
        {
            e.Reply(a);
        }
        Console.WriteLine(e.TextMessage);
    }

    protected override void Unload() { }

    public PluginConfig Config { get; } = new();
}
