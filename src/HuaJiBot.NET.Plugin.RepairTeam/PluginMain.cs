using System.Text;
using HuaJiBot.NET.Commands;
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
            Service.SendGroupMessage(null, Config.PushRawGroup, msg);
        if (Config.PushInfoGroup.Any())
        {
            var e = JsonConvert.DeserializeObject<LogEventEntity>(msg)!;
            var sb = new StringBuilder();
            sb.AppendLine($"---维修事件---");
            sb.AppendLine($"ID：{e.EventId}");
            sb.AppendLine($"类型：{e.Action}");
            sb.AppendLine($"时间：{e.GmtCreate.ToString("f")}");
            if (!string.IsNullOrWhiteSpace(e.MemberId))
                sb.AppendLine($"人员：{e.MemberId}({e.MemberAlias})");
            if (!string.IsNullOrWhiteSpace(e.Model))
                sb.AppendLine($"机型：{e.Model}");
            if (!string.IsNullOrWhiteSpace(e.Problem))
                sb.AppendLine($"问题：{e.Problem}");
            if (!string.IsNullOrWhiteSpace(e.Description))
                sb.AppendLine($"描述：{e.Description}");
            var str = sb.ToString();
            Task.Run(() =>
            {
                foreach (var group in Config.PushInfoGroup)
                {
                    Service.SendGroupMessage(null, group, str);
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
    //[Command("test", "")]
    //private void TestCommand(
    //    [CommandArgumentString("test")] string a,
    //    [CommandArgumentEnum<TEST>("test")] TEST b
    //) { }
    protected override void Unload() { }

    public PluginConfig Config { get; } = new();
}
