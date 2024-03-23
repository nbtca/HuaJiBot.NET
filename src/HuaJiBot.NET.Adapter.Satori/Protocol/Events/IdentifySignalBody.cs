using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.Satori.Protocol.Events;

public class IdentifySignalBody
{
    /// <summary>
    /// 鉴权令牌
    /// </summary>
    [JsonProperty("token")]
    public string? Token { get; set; }

    /// <summary>
    /// 序列号
    /// </summary>
    [JsonProperty("sequence")]
    public string? Sequence { get; set; }
}
