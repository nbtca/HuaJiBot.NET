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
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(10);
    private MessageHistory _history = null!;
    private DailySummaryTask _summaryTask = null!;
    private readonly ConcurrentDictionary<string, byte> _inFlight = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cooldown = new();

    public PluginConfig Config { get; } = new();

    private AgentConnector Connector => AgentConnector.Create(Service, Config.Model);

    protected override Task InitializeAsync()
    {
        _history = new MessageHistory(Service, "daily_summary_messages.db");

        // 记录群聊消息
        Service.Events.OnGroupMessageReceived += (_, e) => OnGroupMessageReceived(e);

        // 启动定时总结任务
        _summaryTask = new DailySummaryTask(
            Service,
            Config,
            Path.Combine(Service.GetPluginDataPath(), "daily_summary_sent.json"),
            async (groupId, date, ct) =>
            {
                // 前天 is the oldest day the 总结 command can ask for.
                _history.DeleteBefore(DayStart(date.AddDays(-1)));
                await SummarizeAsync(groupId, date, ct);
            }
        );
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

    // The distroless image has no tzdata, so DateTime.Now is UTC; split days at UTC+8.
    private static DateTime DayStart(DateTime date) =>
        new DateTimeOffset(date, Utils.NetworkTime.LocalTimeZoneOffset).LocalDateTime;

    /// <returns>false when the day had too few messages to summarize.</returns>
    private async Task<bool> SummarizeAsync(string groupId, DateTime date, CancellationToken ct)
    {
        if (!_inFlight.TryAdd(groupId, 0))
            throw new InvalidOperationException("上一条总结仍在进行中");
        try
        {
            var start = DayStart(date);
            var messages = _history
                .GetGroupMessagesByTimeRange(groupId, start, start.AddDays(1), int.MaxValue)
                .OrderBy(m => m.Timestamp)
                .ToList();

            if (messages.Count < Config.MinMessageCount)
            {
                Info($"群组 {groupId} {date:yyyy-MM-dd} 消息数量不足 ({messages.Count}/{Config.MinMessageCount})，跳过总结");
                return false;
            }

            var summary = await SummaryPipeline.RunAsync(
                messages,
                date,
                Config.MaxPromptChars,
                Config.SystemPrompt,
                InvokeLlmAsync,
                ct
            );

            var text = $"📊 **每日聊天总结** ({date:yyyy-MM-dd})\n\n{summary}";
            var content = new RichContent(text);
            await Service.SendRichMessageAsync(
                null,
                groupId,
                content,
                () => Task.FromResult<SendingMessageBase[]>([content.ToPlainText()])
            );

            Info($"已发送群组 {groupId} 的 {date:yyyy-MM-dd} 总结");
            return true;
        }
        finally
        {
            _inFlight.TryRemove(groupId, out _);
        }
    }

    private async Task<string> InvokeLlmAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct
    )
    {
        var connector = Connector;
        var session = await connector.CreateSessionAsync(ct);
        var agent = connector.CreateAIAgent(systemPrompt);
        var response = await agent.RunAsync(
            [new ChatMessage(ChatRole.User, userMessage)],
            session,
            cancellationToken: ct
        );
        return string.IsNullOrWhiteSpace(response.Text)
            ? throw new InvalidOperationException("AI 返回了空总结")
            : response.Text;
    }

    [Command("总结", "总结群聊记录，可选今天、昨天、前天，默认昨天")]
    // ReSharper disable once UnusedMember.Global
    public async Task SummaryCommand(
        GroupMessageEventArgs e,
        [CommandArgumentEnum<SummaryTarget>("日期")] SummaryTarget? target = null
    )
    {
        if (!Config.GroupIds.Contains(e.GroupId))
            return;

        var now = Utils.NetworkTime.Now;
        if (_cooldown.TryGetValue(e.SenderId, out var last) && now - last < Cooldown)
        {
            await e.Reply($"我知道你很急，但是你先别急，{(Cooldown - (now - last)).TotalSeconds:F0}秒后再试");
            return;
        }
        _cooldown[e.SenderId] = now;
        _ = Task.Delay(Cooldown).ContinueWith(_ => _cooldown.TryRemove(e.SenderId, out var _));

        var date = now.Date.AddDays(-(int)(target ?? SummaryTarget.昨天));
        await e.Reply($"收到，正在总结 {date:yyyy-MM-dd} 的聊天记录…");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Config.LlmTimeoutSeconds));
        try
        {
            if (!await SummarizeAsync(e.GroupId, date, cts.Token))
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

    protected override void Unload()
    {
        _summaryTask?.Dispose();
        _history?.Dispose();
    }
}
