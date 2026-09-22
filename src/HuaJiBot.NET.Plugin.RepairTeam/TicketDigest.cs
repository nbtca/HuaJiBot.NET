using System.Text;

namespace HuaJiBot.NET.Plugin.RepairTeam;

internal static class TicketDigest
{
    public const string PortalUrl = "https://repair.nbtca.space";

    private static readonly (string Status, string Title)[] Stages =
    [
        ("open", "待接单"),
        ("accepted", "已接单未提交"),
        ("committed", "待审核"),
    ];

    private static double ThresholdDays(string status, PluginConfig config) =>
        status switch
        {
            "open" => config.OpenDays,
            "accepted" => config.AcceptedDays,
            "committed" => config.CommittedDays,
            _ => double.PositiveInfinity,
        };

    public static List<Ticket> Stalled(IEnumerable<Ticket> tickets, DateTimeOffset now, PluginConfig config) =>
        tickets.Where(t => (now - t.LastActivity).TotalDays > ThresholdDays(t.Status, config)).ToList();

    public static string Format(string title, IReadOnlyCollection<Ticket> tickets, DateTimeOffset now)
    {
        var sb = new StringBuilder(title).Append('\n');
        foreach (var (status, stageTitle) in Stages)
        {
            var stage = tickets.Where(t => t.Status == status).OrderBy(t => t.LastActivity).ToList();
            if (stage.Count == 0)
                continue;
            sb.Append(stageTitle).Append("：\n");
            foreach (var t in stage)
            {
                var days = (int)(now - t.LastActivity).TotalDays;
                var fields = new[] { t.Model, t.Problem, t.Member, days < 1 ? "不到 1 天" : $"已 {days} 天" };
                sb.Append($"  #{t.Id} ").AppendJoin(" · ", fields.Where(f => !string.IsNullOrWhiteSpace(f))).Append('\n');
            }
        }
        return sb.Append($"去处理：{PortalUrl}").ToString();
    }
}
