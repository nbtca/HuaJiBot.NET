using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HuaJiBot.NET.AI;

[JsonConverter(typeof(StringEnumConverter))]
public enum ModelProvider
{
    // ReSharper disable once InconsistentNaming
    OpenAI,
    Google,
}
