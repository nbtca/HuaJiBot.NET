using HuaJiBot.NET.AI;

namespace HuaJiBot.NET.Plugin.DailySummary.Config;

public class PluginConfig : ConfigBase
{
    /// <summary>
    /// 总结时间 - 小时（北京时间，24小时制），失败的群在这一小时内重试
    /// </summary>
    public int SummaryHour = 0;

    /// <summary>
    /// 总结时间 - 分钟
    /// </summary>
    public int SummaryMinute = 0;

    /// <summary>
    /// 最小消息数量，低于此值不生成总结
    /// </summary>
    public int MinMessageCount = 10;

    /// <summary>
    /// 每个群每天定时总结的最大尝试次数
    /// </summary>
    public int MaxRetryCount = 3;

    /// <summary>
    /// 单次总结的超时秒数
    /// </summary>
    public int LlmTimeoutSeconds = 180;

    /// <summary>
    /// 发给模型的聊天记录最大字符数，超出时只保留最新的部分
    /// </summary>
    public int MaxPromptChars = 20000;

    /// <summary>
    /// AI 系统提示词
    /// </summary>
    public string SystemPrompt =
        "你是一个聊天记录总结助手。请对以下群聊消息进行简洁的总结，突出重要的讨论话题、决策和有趣的内容。使用中文回复。若消息中声明了截断，请只基于给出的内容总结，不要臆测缺失部分。";

    /// <summary>
    /// AI 模型配置
    /// </summary>
    public ModelConfig Model = new();

    /// <summary>
    /// Groups to record and summarize. Empty records no group.
    /// </summary>
    public List<string> GroupIds = [];
}
