using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HuaJiBot.NET.Plugin.MessageBridge.Types;

public abstract class BasePacket
{
    public abstract PacketType Type { get; }
    public required SenderInformation? Source { get; init; }

    public string ToJson() => JsonConvert.SerializeObject(this, SerializerSettings.Value);

    public static BasePacket? FromJson(JObject json) =>
        json.ToObject<BasePacket>(JsonSerializer.Create(SerializerSettings.Value));

    public static BasePacket? FromJson(string json) =>
        JsonConvert.DeserializeObject<BasePacket>(json, SerializerSettings.Value);

    private static readonly Lazy<JsonSerializerSettings> SerializerSettings = new(() =>
        new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(),
            },
            Converters = [new PacketConverter()],
        }
    );

    [JsonIgnore]
    public static SenderInformation? DefaultInformation = new(
        "QQGroup",
        "HuaJiBot.NET.Plugin.MessageBridge",
        "?"
    );
}

public abstract class DataPacket<T>(PacketType type) : BasePacket
{
    public sealed override PacketType Type => type;
    public required T Data { get; init; }
}

public record SenderInformation(string DisplayName, string Name, string Version);
