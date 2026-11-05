using System.Collections.Concurrent;
using System.Text;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Plugin.MessageBridge.Types;
using HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

namespace HuaJiBot.NET.Plugin.MessageBridge;

partial class PluginMain
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<GetPlayerListResponsePacket>> _requestQueue = new();

    [Command("查询", "查询MC服务器状态", Alias = "mc")]
    // ReSharper disable once UnusedMember.Local
    private async Task QueryAsync(GroupMessageEventArgs e)
    {
        var list = await InvokeQueryAsync(10_000);
        await ReplyPlayerListAsync(e, list);
    }

    internal static async Task ReplyPlayerListAsync(GroupMessageEventArgs e, GetPlayerListResponsePacket[] list)
    {
        foreach (var item in list)
        {
            if (!item.Data.Players.Any())
            {
                await e.Reply($"{item.Source?.DisplayName} 没有玩家在线！");
                continue;
            }
            var s = new StringBuilder();
            s.AppendLine($"{item.Source?.DisplayName} 玩家列表：");
            foreach (var player in item.Data.Players)
                s.AppendLine($"{player.Name} {player.Ping}ms {player.World}");
            await e.Reply(s.ToString());
        }
        if (list.Length == 0)
            await e.Reply("查询超时！");
    }

    private async Task<GetPlayerListResponsePacket[]> InvokeQueryAsync(int timeout = 5000)
    {
        try
        {
            var uuid = Guid.NewGuid().ToString("N");
            var pkt = new GetPlayerListRequestPacket
            {
                Data = new() { RequestId = uuid },
                Source = BasePacket.DefaultInformation,
            };
            var req = pkt.ToJson();
            _requestQueue.TryAdd(uuid, []);
            foreach (var (_, connection) in _clients)
            {
                connection.Send(req);
            }
            await Task.Delay(timeout);
            if (_requestQueue.TryRemove(uuid, out var item))
            {
                return item.ToArray();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        return [];
    }

    private void ProcessPlayerListResponse(GetPlayerListResponsePacket pkt)
    {
        var uuid = pkt.Data.RequestId;
        if (_requestQueue.TryGetValue(uuid, out var item))
        {
            item.Add(pkt);
        }
    }
}
