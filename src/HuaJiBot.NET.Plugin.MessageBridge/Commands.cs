using System.Collections.Concurrent;
using System.Text;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Plugin.MessageBridge.Types;
using HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;

namespace HuaJiBot.NET.Plugin.MessageBridge;

partial class PluginMain
{
    private readonly ConcurrentDictionary<
        string,
        (
            Func<GetPlayerListResponsePacket, ValueTask>? callback,
            ConcurrentBag<GetPlayerListResponsePacket> data
        )
    > _requestQueue = new();

    [Command("查询", "查询MC服务器状态")]
    // ReSharper disable once UnusedMember.Local
    private async Task QueryAsync(GroupMessageEventArgs e)
    {
        var list = await InvokeQueryAsync(
            10_000,
            item =>
            {
                if (!item.Data.Players.Any())
                {
                    e.Reply($"{item.Source?.DisplayName} 没有玩家在线！");
                    return ValueTask.CompletedTask;
                }
                var s = new StringBuilder();
                s.AppendLine($"{item.Source?.DisplayName} 玩家列表：");
                foreach (var player in item.Data.Players)
                {
                    s.AppendLine($"{player.Name} {player.Ping}ms {player.World}");
                }
                e.Reply(s.ToString());
                return ValueTask.CompletedTask;
            }
        );
        if (list.Length == 0)
            e.Reply("查询超时！");
    }

    private async Task<GetPlayerListResponsePacket[]> InvokeQueryAsync(
        int timeout = 5000,
        Func<GetPlayerListResponsePacket, ValueTask>? callback = null
    )
    {
        try
        {
            var uuid = Guid.NewGuid().ToString("N");
            var pkt = new GetPlayerListRequestPacket
            {
                Data = new() { RequestId = uuid },
                Source = BasePacket.DefaultInformation
            };
            var req = pkt.ToJson();
            _requestQueue.TryAdd(uuid, (callback, new()));
            foreach (var (_, connection) in _clients)
            {
                connection.Send(req);
            }
            await Task.Delay(timeout);
            if (_requestQueue.TryRemove(uuid, out var item))
            {
                return item.data.ToArray();
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
            item.data.Add(pkt);
            item.callback?.Invoke(pkt);
        }
    }

    private void ProcessActiveBroadcast(ActiveBroadcastPacket activeBroadcast)
    {
        Info(activeBroadcast.ToJson());
    }
}
