using HuaJiBot.NET.Plugin.DailySummary;

[assembly: HuaJiBot.NET.PluginEntryPoint<PluginMain>(
    "每日聊天总结",
    "每天定时总结群聊消息，使用AI生成摘要并发送到群聊")]