namespace HuaJiBot.NET.Plugin.DailySummary.Config;

public class PluginConfig : ConfigBase
{
    /// <summary>
    /// 总结时间 - 小时（24小时制）
    /// </summary>
    public int SummaryHour = 0;

    /// <summary>
    /// 总结时间 - 分钟
    /// </summary>
    public int SummaryMinute = 0;

    /// <summary>
    /// 最小消息数量阈值，只有消息数量超过此值才会生成总结
    /// </summary>
    public int MinMessageCount = 10;

    /// <summary>
    /// AI 系统提示词
    /// </summary>
    public string SystemPrompt = "你是一个聊天记录总结助手。请对以下群聊消息进行简洁的总结，突出重要的讨论话题、决策和有趣的内容。使用中文回复。";

    /// <summary>
    /// AI 模型配置
    /// </summary>
    public ModelConfig Model = new();

    /// <summary>
    /// 监听的群组列表（为空则监听所有群组）
    /// </summary>
    public List<long> GroupIds = [];
}