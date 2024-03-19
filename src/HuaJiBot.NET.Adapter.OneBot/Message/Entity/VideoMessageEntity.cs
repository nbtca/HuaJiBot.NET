using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class VideoMessageEntity(string url) : MessageEntity
{
    public VideoMessageEntity()
        : this("") { }

    [JsonProperty("file")]
    public string File { get; set; } = url;

    [JsonProperty("url")]
    public string Url { get; set; } = url;

    public override JObject ToJson() => new() { ["file"] = File };
}
