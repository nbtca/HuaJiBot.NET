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
    public int LlmTimeoutSeconds = 600;

    /// <summary>
    /// 每个分段发给模型的聊天记录最大字符数，超出时分段总结并合并
    /// </summary>
    public int MaxPromptChars = 20000;

    /// <summary>
    /// AI 系统提示词
    /// </summary>
    public string SystemPrompt =
        "你是群聊简报编辑。按时间顺序理解记录，合并重复话题，区分事实、猜测与玩笑。只写有聊天依据的内容，不编造决定或待办。";

    /// <summary>
    /// AI 模型配置
    /// </summary>
    public ModelConfig Model = new();

    /// <summary>
    /// Groups to record and summarize. Empty records no group.
    /// </summary>
    public List<string> GroupIds = [];
}
