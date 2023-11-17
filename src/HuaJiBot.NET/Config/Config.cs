using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Config;

public partial class Config
{
    public string[] ExtraPlugins { get; set; } = { };
    public Dictionary<string, JObject> Plugins = new();
}
