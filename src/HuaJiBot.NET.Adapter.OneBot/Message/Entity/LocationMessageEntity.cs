using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class LocationMessageEntity(float latitude, float longitude) : MessageEntity
{
    public LocationMessageEntity()
        : this(0f, 0f) { }

    [JsonProperty("lat")]
    public string Latitude { get; set; } = latitude.ToString("F5");

    [JsonProperty("lon")]
    public string Longitude { get; set; } = longitude.ToString("F5");

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}
