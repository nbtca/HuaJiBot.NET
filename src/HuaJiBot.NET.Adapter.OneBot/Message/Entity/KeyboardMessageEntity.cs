using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class KeyboardMessageEntity(KeyboardMessageEntity.KeyboardData content) : MessageEntity
{
    public KeyboardMessageEntity()
        : this(new KeyboardData()) { }

    [JsonProperty("content")]
    public KeyboardData Content { get; set; } = content;

    public class KeyboardData
    {
        [JsonProperty("rows")]
        public List<Row> Rows { get; set; } = [];
    }

    public class Row
    {
        [JsonProperty("buttons")]
        public List<Button> Buttons { get; set; } = [];
    }

    public class Button
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("render_data")]
        public RenderData RenderData { get; set; } = new();

        [JsonProperty("action")]
        public Action Action { get; set; } = new();
    }

    public class Action
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("permission")]
        public Permission Permission { get; set; } = new();

        // [JsonProperty("click_limit")]
        // public long ClickLimit { get; set; }

        [JsonProperty("unsupport_tips")]
        public string UnsupportTips { get; set; } = string.Empty;

        [JsonProperty("data")]
        public string Data { get; set; } = string.Empty;

        [JsonProperty("reply")]
        public bool? Reply { get; set; }

        [JsonProperty("enter")]
        public bool? Enter { get; set; }
    }

    public class Permission
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("specify_role_ids")]
        public List<string> SpecifyRoleIds { get; set; } = [];

        [JsonProperty("specify_user_ids")]
        public List<string> SpecifyUserIds { get; set; } = [];
    }

    public class RenderData
    {
        [JsonProperty("label")]
        public string Label { get; set; } = string.Empty;

        [JsonProperty("visited_label")]
        public string VisitedLabel { get; set; } = string.Empty;

        [JsonProperty("style")]
        public int Style { get; set; }
    }

    public override JObject ToJson()
    {
        throw new NotImplementedException();
    }
}
