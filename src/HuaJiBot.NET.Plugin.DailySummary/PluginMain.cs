using HuaJiBot.NET.Bot;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Plugin.DailySummary.Config;
using HuaJiBot.NET.Plugin.DailySummary.Service;
using HuaJiBot.NET.Plugin.DailySummary.Service.Connector;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using System.Text;

namespace HuaJiBot.NET.Plugin.DailySummary;

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    private MessageHistory _history = null!;
    private DailySummaryTask _summaryTask = null!;

    public PluginConfig Config { get; } = new();

    private AgentConnector Connector
    {
        get
        {
            return Config.Model switch
            {
                { Provider: ModelProvider.OpenAI } => new OpenAIAgentConnector(
                    Service,
                    Config.Model
                ),
                { Provider: ModelProvider.Google } => new GoogleAgentConnector(
                    Service,
                    Config.Model
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(Config.Model.Provider)),
            };
        }
    }

    protected override async Task InitializeAsync()
    {
        _history = new MessageHistory(Service, "daily_summary_messages.db");

        // 记录群聊消息
        Service.Events.OnGroupMessageReceived += (_, e) => OnGroupMessageReceived(e);

        // 启动定时总结任务
        _summaryTask = new DailySummaryTask(Service, Config, GenerateSummaryAsync);
        _summaryTask.Start();

        Info("启动成功");
    }

    private void OnGroupMessageReceived(Events.GroupMessageEventArgs e)
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
                SenderName = e.SenderMemberCard ?? "未知用户",
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

    private async Task GenerateSummaryAsync(string groupId)
    {
        try
        {
            // 基础镜像没有 tzdata，DateTime.Now 是 UTC，日界按 NetworkTime 的 UTC+8 计算
            var today = Utils.NetworkTime.Now.Date;
            var yesterday = today.AddDays(-1);
            var offset = Utils.NetworkTime.LocalTimeZoneOffset;
            var messages = _history
                .GetGroupMessagesByTimeRange(
                    groupId,
                    new DateTimeOffset(yesterday, offset).LocalDateTime,
                    new DateTimeOffset(today, offset).LocalDateTime,
                    int.MaxValue
                )
                .ToList();

            // 检查消息数量
            if (messages.Count < Config.MinMessageCount)
            {
                Info($"群组 {groupId} 消息数量不足 ({messages.Count}/{Config.MinMessageCount})，跳过总结");
                return;
            }

            // 构建消息内容
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("以下是昨天的群聊记录，请进行总结：\n");

            foreach (var msg in messages)
            {
                var time = new DateTimeOffset(msg.Timestamp)
                    .ToOffset(offset)
                    .ToString("HH:mm");
                var sender = msg.IsBot ? "机器人" : msg.SenderName;
                messageBuilder.AppendLine($"[{time}] {sender}: {msg.Content}");
            }

            // 调用 AI 生成总结
            var summary = await InvokeLlmAsync(messageBuilder.ToString());

            var content = new RichContent(
                $"📊 **每日聊天总结** ({yesterday:yyyy-MM-dd})\n\n{summary}"
            );
            await Service.SendRichMessageAsync(
                null,
                groupId,
                content,
                () => Task.FromResult<SendingMessageBase[]>([content.ToPlainText()])
            );

            Info($"已发送群组 {groupId} 的每日总结");
        }
        catch (Exception ex)
        {
            Error($"群组 {groupId} 生成总结失败", ex);
        }
    }

    private async Task<string> InvokeLlmAsync(string userMessage)
    {
        var connector = Connector;
        var session = await connector.CreateSessionAsync();
        var agent = connector.CreateAIAgentWithOptions(
            Config.SystemPrompt,
            functionTools: null,
            mcpTools: null
        );
        var response = await agent.RunAsync([new ChatMessage(ChatRole.User, userMessage)], session);
        return string.IsNullOrWhiteSpace(response.Text)
            ? throw new InvalidOperationException("AI 返回了空总结")
            : response.Text;
    }

    protected override void Unload()
    {
        _summaryTask?.Dispose();
    }
}
