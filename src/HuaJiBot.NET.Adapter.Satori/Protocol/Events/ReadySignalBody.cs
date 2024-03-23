using HuaJiBot.NET.Adapter.Satori.Protocol.Models;

namespace HuaJiBot.NET.Adapter.Satori.Protocol.Events;

public class ReadySignalBody
{
    public IEnumerable<Login> Logins { get; set; } = Array.Empty<Login>();
}
