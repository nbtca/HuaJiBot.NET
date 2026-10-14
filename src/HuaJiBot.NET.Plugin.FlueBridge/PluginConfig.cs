namespace HuaJiBot.NET.Plugin.FlueBridge;

public class PluginConfig : ConfigBase
{
    public PluginConfig() => Enabled = false;
    public string BridgeInstance { get; set; } = "main";
    public string IngressUrl { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string QueueId { get; set; } = "";
    public List<BridgeDestination> Destinations { get; set; } = [];
    public int MaxMessageBytes { get; set; } = 32768;
    public int RetentionDays { get; set; } = 30;
    public int ActiveWindowSeconds { get; set; } = 120;
    public int IdlePollSeconds { get; set; } = 15;
    public int RetryDelaySeconds { get; set; } = 10;
    public int VisibilityTimeoutSeconds { get; set; } = 120;

    internal bool Allows(string robotId, string groupId) =>
        Destinations.Any(d => d.RobotId == robotId && d.GroupId == groupId);

    internal void Validate()
    {
        if (!Uri.TryCreate(IngressUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("FlueBridge requires an HTTPS ingress URL.");
        if (string.IsNullOrWhiteSpace(BridgeInstance) || string.IsNullOrWhiteSpace(AccountId)
            || string.IsNullOrWhiteSpace(QueueId) || Destinations.Count == 0
            || Destinations.Any(d => string.IsNullOrWhiteSpace(d.RobotId) || string.IsNullOrWhiteSpace(d.GroupId))
            || MaxMessageBytes is < 1 or > 65536 || RetentionDays < 1
            || ActiveWindowSeconds < 1 || IdlePollSeconds < 1 || RetryDelaySeconds is < 1 or > 43200
            || VisibilityTimeoutSeconds is < 30 or > 43200)
            throw new InvalidOperationException("Invalid FlueBridge configuration.");
    }
}

public record BridgeDestination(string RobotId, string GroupId);
