namespace HuaJiBot.NET.AI;

public record ModelConfig(
    ModelProvider Provider = ModelProvider.OpenAI,
    string Endpoint = "",
    string ModelId = "",
    string ApiKey = "",
    string? AgentName = "Bot",
    bool Logging = false
);
