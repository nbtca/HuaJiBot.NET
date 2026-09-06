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
        Service.Events.OnGroupMessageReceived += (s, e) => _ = OnGroupMessageReceived(e);

        // 启动定时总结任务
        _summaryTask = new DailySummaryTask(Service, Config, GenerateSummaryAsync);
        _summaryTask.Start();

        Info("启动成功");
    }

    private async Task OnGroupMessageReceived(Events.GroupMessageEventArgs e)
    {
        try
        {
            // 检查是否在监听的群组列表中
            if (Config.GroupIds.Count > 0 && !Config.GroupIds.Contains(long.Parse(e.GroupId)))
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
            Error("记录消息失败", ex.Message);
        }
    }

    private async Task GenerateSummaryAsync(string groupId)
    {
        try
        {
            // 获取昨天的消息
            var now = DateTime.Now;
var yesterday = now.AddDays(-1);
            var messages = _history.GetMessagesByTimeRange(yesterday, now).ToList();

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
                var time = msg.Timestamp.ToString("HH:mm");
                var sender = msg.IsBot ? "机器人" : msg.SenderName;
                messageBuilder.AppendLine($"[{time}] {sender}: {msg.Content}");
            }

            // 调用 AI 生成总结
            var summary = await InvokeLlmAsync(messageBuilder.ToString());

            // 发送总结到群组
            var header = $"📊 **每日聊天总结** ({yesterday:yyyy-MM-dd})\n\n";
            await Service.SendGroupMessageAsync(groupId, header + summary);

            Info($"已发送群组 {groupId} 的每日总结");
        }
        catch (Exception ex)
        {
            Error("生成总结失败", ex.Message);
        }
    }

    private async Task<string> InvokeLlmAsync(string userMessage)
    {
        try
        {
            var connector = Connector;
            var session = await connector.CreateSessionAsync();
            var agent = connector.CreateAIAgentWithOptions(
                Config.SystemPrompt,
                functionTools: null,
                mcpTools: null
            );

            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, userMessage)
            };

            var response = await agent.RunAsync(messages, session);
            return response.Text ?? "无法生成总结";
        }
        catch (Exception ex)
        {
            Error("AI调用失败", ex.Message);
            return $"AI调用失败: {ex.Message}";
        }
    }

    protected override void Unload()
    {
        _summaryTask?.Dispose();
    }
}