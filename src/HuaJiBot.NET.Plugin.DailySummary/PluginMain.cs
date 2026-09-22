using System.Collections.Concurrent;
using HuaJiBot.NET.AI;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Plugin.DailySummary.Config;
using HuaJiBot.NET.Plugin.DailySummary.Service;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HuaJiBot.NET.Plugin.DailySummary;

public partial class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    private MessageHistory _history = null!;
    private DailySummaryTask _summaryTask = null!;
    private SummaryState _state = null!;
    private readonly ConcurrentDictionary<string, byte> _groupsInFlight = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cooldown = new();
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    public PluginConfig Config { get; } = new();

    private AgentConnector Connector => AgentConnector.Create(Service, Config.Model);

    protected override Task InitializeAsync()
    {
        _history = new MessageHistory(Service, "daily_summary_messages.db");
        _state = new SummaryState(
            Path.Combine(Service.GetPluginDataPath(), "daily_summary_state.json")
        );

        // 记录群聊消息
        Service.Events.OnGroupMessageReceived += (_, e) => OnGroupMessageReceived(e);

        // 启动定时总结任务
        _summaryTask = new DailySummaryTask(Service, Config, _state, GenerateAndSendAsync);
        _summaryTask.Start();

        Info("启动成功");
        return Task.CompletedTask;
    }

    private void OnGroupMessageReceived(GroupMessageEventArgs e)
    {
        try
        {
            if (!Config.GroupIds.Contains(e.GroupId))
                return;

            // 存储消息
            _history.StoreMessage(new GroupMessage
            {
                MessageId = e.MessageId,
                GroupId = e.GroupId,
                SenderId = e.SenderId,
                SenderName = e.SenderMemberCard,
                Content = e.TextMessage,
                IsBot = false,
                ReplyToMessageId = null,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            Error("记录消息失败", ex);
        }
    }

    private async Task<bool> GenerateAndSendAsync(
        string groupId,
        DateTime targetDate,
        CancellationToken ct
    )
    {
        // 定时任务与手动命令共用同一把群级锁；撞车时让定时任务走重试路径。
        if (!_groupsInFlight.TryAdd(groupId, 0))
            throw new InvalidOperationException($"群组 {groupId} 上一条总结仍在进行中");
        try
        {
            return await GenerateCoreAsync(groupId, targetDate, ct);
        }
        finally
        {
            _groupsInFlight.TryRemove(groupId, out _);
        }
    }

    /// <summary>返回 false 表示当日消息数量不足，无需重试。</summary>
    private async Task<bool> GenerateCoreAsync(
        string groupId,
        DateTime targetDate,
        CancellationToken ct
    )
    {
        // 镜像无 tzdata 时 DateTime.Now 是 UTC；把 +8 的日界经 LocalDateTime 换算，
        // 与存储时间戳在同一台机器上始终同一挂钟，任意主机时区下都自洽。
        var offset = Utils.NetworkTime.LocalTimeZoneOffset;
        var start = new DateTimeOffset(targetDate, offset).LocalDateTime;
        var messages = _history
            .GetGroupMessagesByTimeRange(groupId, start, start.AddDays(1), int.MaxValue)
            .OrderBy(m => m.Timestamp)
            .ToList();

        if (messages.Count < Config.MinMessageCount)
        {
            Info($"群组 {groupId} {targetDate:yyyy-MM-dd} 消息数量不足 ({messages.Count}/{Config.MinMessageCount})，跳过总结");
            return false;
        }

        var prompt = SummaryPrompt.Build(messages, Config.MaxPromptChars, targetDate);
        var summary = await InvokeLlmAsync(prompt.Text, ct);

        var text = $"📊 **每日聊天总结** ({targetDate:yyyy-MM-dd})\n\n{summary}";
        if (prompt is { Truncated: true, KeptFrom: not null })
        {
            var keptFrom = new DateTimeOffset(prompt.KeptFrom.Value)
                .ToOffset(offset)
                .ToString("HH:mm");
            text += $"\n\n⚠️ 当日消息较多，总结仅基于 {keptFrom} 之后的记录。";
        }
        var content = new RichContent(text);
        await Service.SendRichMessageAsync(
            null,
            groupId,
            content,
            () => Task.FromResult<SendingMessageBase[]>([content.ToPlainText()])
        );

        // 发送成功后才清理更早的消息，失败时数据仍在，可重试或手动补发。
        _history.DeleteBefore(start);

        Info($"已发送群组 {groupId} 的 {targetDate:yyyy-MM-dd} 总结");
        return true;
    }

    private async Task<string> InvokeLlmAsync(string userMessage, CancellationToken ct)
    {
        var connector = Connector;
        var session = await connector.CreateSessionAsync(ct);
        var agent = connector.CreateAIAgent(Config.SystemPrompt);
        List<ChatMessage> messages = [new ChatMessage(ChatRole.User, userMessage)];
        var response = await agent.RunAsync(
            messages,
            session,
            options: null,
            cancellationToken: ct
        );
        return string.IsNullOrWhiteSpace(response.Text)
            ? throw new InvalidOperationException("AI 返回了空总结")
            : response.Text;
    }

    [Command("总结", "手动生成聊天总结，参数：今天|昨天|前天（缺省与定时任务同一天）")]
    // ReSharper disable once UnusedMember.Global
    public async Task SummaryCommand(
        GroupMessageEventArgs e,
        [CommandArgumentEnum<SummaryTarget>("总结日期")] SummaryTarget? target = null
    )
    {
        // 白名单外静默忽略：命令是全局的，不能让任意群触发总结或看到提示。
        if (!Config.GroupIds.Contains(e.GroupId))
            return;

        var now = Utils.NetworkTime.Now;
        if (
            _cooldown.TryGetValue(e.SenderId, out var last)
            && now - last < Cooldown
        )
        {
            await e.Reply(
                $"我知道你很急，但是你先别急，{(Cooldown - (now - last)).TotalSeconds:F0}秒后再试"
            );
            return;
        }
        _cooldown[e.SenderId] = now;
        _ = Task.Delay(Cooldown).ContinueWith(
            _ => _cooldown.TryRemove(e.SenderId, out var removed)
        );

        if (!_groupsInFlight.TryAdd(e.GroupId, 0))
        {
            await e.Reply("上一条总结仍在进行中，请稍候");
            return;
        }
        try
        {
            var daysAgo = target switch
            {
                SummaryTarget.今天 => 0,
                SummaryTarget.昨天 => 1,
                SummaryTarget.前天 => 2,
                _ => Math.Max(0, Config.SummaryDaysAgo),
            };
            var targetDate = now.Date.AddDays(-daysAgo);
            // 手动触发不写定时状态：到点的定时任务照发（用户确认的语义）。
            await e.Reply($"收到，正在总结 {targetDate:yyyy-MM-dd} 的聊天记录…");

            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(Math.Max(1, Config.LlmTimeoutSeconds))
            );
            try
            {
                var sent = await GenerateCoreAsync(e.GroupId, targetDate, cts.Token);
                if (!sent)
                    await e.Reply("该日消息数量不足，未生成总结");
            }
            catch (OperationCanceledException)
            {
                await e.Reply($"总结超时（{Config.LlmTimeoutSeconds}s），请稍后再试");
            }
            catch (Exception ex)
            {
                Error($"群组 {e.GroupId} 手动总结失败", ex);
                await e.Reply($"总结失败：{ex.Message}");
            }
        }
        finally
        {
            _groupsInFlight.TryRemove(e.GroupId, out _);
        }
    }

    protected override void Unload()
    {
        _summaryTask?.Dispose();
        _history?.Dispose();
    }
}
