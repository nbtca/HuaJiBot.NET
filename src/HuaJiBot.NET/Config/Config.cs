using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Config;

public partial class Config
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ServiceType
    {
        OneBot,
        Satori,
        Telegram,
    }

    public ServiceType Service { get; set; } = ServiceType.OneBot;

    public class OneBotConnectionInfo
    {
        public string Url { get; set; } = "";
        public string? Token { get; set; } = "";
    }

    public OneBotConnectionInfo OneBot { get; set; } = new();

    public class SatoriConnectionInfo
    {
        public string Url { get; set; } = "";
        public string Token { get; set; } = "";
    }

    public SatoriConnectionInfo Satori { get; set; } = new();

    public class TelegramConnectionInfo
    {
        public string Token { get; set; } = "";
    }

    public TelegramConnectionInfo Telegram { get; set; } = new();

    public string[] ExtraPlugins { get; set; } = [];

    public Dictionary<string, JObject> Plugins { get; set; } = new();
}
