using System.Text.Json.Serialization;
using Newtonsoft.Json;

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
        public List<Row> Rows { get; set; } = new();
    }

    public class Row
    {
        [JsonProperty("buttons")]
        public List<Button> Buttons { get; set; } = new();
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
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("permission")]
        public Permission Permission { get; set; } = new();

        // [JsonPropertyName("click_limit")]
        // public long ClickLimit { get; set; }

        [JsonPropertyName("unsupport_tips")]
        public string UnsupportTips { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        [JsonPropertyName("reply")]
        public bool? Reply { get; set; }

        [JsonPropertyName("enter")]
        public bool? Enter { get; set; }
    }

    public class Permission
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("specify_role_ids")]
        public List<string> SpecifyRoleIds { get; set; } = new();

        [JsonPropertyName("specify_user_ids")]
        public List<string> SpecifyUserIds { get; set; } = new();
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
}
