using HuaJiBot.NET.AI;

namespace HuaJiBot.NET.Plugin.DailySummary.Config;

public class PluginConfig : ConfigBase
{
    /// <summary>
    /// 执行总结的小时（北京时间 UTC+8，24小时制）。
    /// 该小时内的每一分钟都会检查，未成功的群会自动重试。
    /// </summary>
    public int SummaryHour = 0;

    /// <summary>
    /// 开始执行/重试的分钟（北京时间）
    /// </summary>
    public int SummaryMinute = 0;

    /// <summary>
    /// 总结哪一天：2 = 前天，1 = 昨天，0 = 今天（调试用）
    /// </summary>
    public int SummaryDaysAgo = 2;

    /// <summary>
    /// 最小消息数量阈值，低于此值不生成总结（视为当天已完成，不再重试）
    /// </summary>
    public int MinMessageCount = 10;

    /// <summary>
    /// 单个群每天定时总结的最大尝试次数，超过后当天放弃
    /// </summary>
    public int MaxRetryCount = 3;

    /// <summary>
    /// 单次 LLM 调用的超时秒数
    /// </summary>
    public int LlmTimeoutSeconds = 180;

    /// <summary>
    /// 同时总结的群数量上限
    /// </summary>
    public int MaxConcurrentGroups = 2;

    /// <summary>
    /// 喂给模型的消息文本最大字符数，超出时保留最新的部分并在提示中声明已截断
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
    /// 需要记录并总结的群号列表，空列表不记录任何群
    /// </summary>
    public List<string> GroupIds = [];
}
