using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace HuaJiBot.NET.Plugin.MessageBridge.Types;

public abstract class BasePacket
{
    public abstract PacketType Type { get; }
    public abstract object? DataObject { get; }
    public required SenderInformation? Source { get; init; }

    public string ToJson() => JsonConvert.SerializeObject(this, SerializerSettings.Value);

    public static BasePacket? FromJson(string json) =>
        JsonConvert.DeserializeObject<BasePacket>(json, SerializerSettings.Value);

    private static readonly Lazy<JsonSerializerSettings> SerializerSettings =
        new(
            () =>
                new()
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    },
                    Converters = [new PacketConverter()]
                }
        );
}

public abstract class DataPacket<T>(PacketType type) : BasePacket
{
    public sealed override PacketType Type => type;
    public required T Data { get; init; }
    public override object? DataObject => Data;
}

public record SenderInformation(string DisplayName, string Name, string Version);
