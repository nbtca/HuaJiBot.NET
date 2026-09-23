using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Plugin.Calendar;
using HuaJiBot.NET.Plugin.MessageBridge;
using HuaJiBot.NET.Plugin.RepairTeam;
using Ical.Net.CalendarComponents;
using Newtonsoft.Json;
using RepairPlugin = HuaJiBot.NET.Plugin.RepairTeam.PluginMain;

namespace HuaJiBot.NET.UnitTest;

internal class PluginPostTest
{
    private static readonly DateTimeOffset Start = new(2026, 9, 25, 20, 30, 0, TimeSpan.FromHours(8));

    [Test]
    public async Task CalendarStart_ShowsTitleTimePlaceAndQuotedDescription()
    {
        var e = new CalendarEvent { Summary = "NWDC_x", Location = "实验室", Description = "带上电脑\n别迟到" };

        var post = ReminderTask.StartPost(Start, Start.AddHours(2), e, 60, "old");
        var fallback = await post.Fallback();

        Assert.Multiple(() =>
        {
            Assert.That(
                post.Markdown,
                Is.EqualTo("⏰ **NWDC\\_x** 60 分钟后开始\n🕘 09-25 20:30–22:30 · 📍 实验室\n\n> 带上电脑\n> 别迟到")
            );
            Assert.That(post.Tags, Is.EqualTo(new[] { "日程" }));
            Assert.That(post.Silent, Is.False);
            Assert.That(fallback, Is.EqualTo(new SendingMessageBase[] { new TextMessage("old") }));
        });
    }

    [Test]
    public void CalendarEnd_IsSilent()
    {
        var post = ReminderTask.EndPost(new CalendarEvent { Summary = "例会" }, 5, "old");

        Assert.Multiple(() =>
        {
            Assert.That(post.Markdown, Is.EqualTo("🔚 **例会** 预计 5 分钟后结束"));
            Assert.That(post.Silent, Is.True);
        });
    }

    [Test]
    public async Task RepairEvent_ShowsTicketAndLinksPortal()
    {
        var e = JsonConvert.DeserializeObject<RepairPlugin.LogEventEntity>(
            """
            {"event_id":900001,"action":"create","member_id":"","member_alias":"张三",
             "gmt_create":"2026-09-21T03:45:53Z","model":"ThinkPad","problem":"无法开机","description":"进水了"}
            """
        )!;

        var post = RepairPlugin.EventPost(e);
        var fallback = await post.Fallback();

        Assert.Multiple(() =>
        {
            Assert.That(
                post.Markdown,
                Is.EqualTo("🛠 **#900001 新报修**\nThinkPad · 无法开机\n\n> 进水了\n\n张三 · 09-21 11:45")
            );
            Assert.That(post.Tags, Is.EqualTo(new[] { "维修", "新报修" }));
            Assert.That(post.Links, Is.EqualTo(new[] { new LinkMessage("去处理", TicketDigest.PortalUrl) }));
            Assert.That(fallback, Is.EqualTo(new SendingMessageBase[] { new TextMessage(RepairPlugin.FormatEvent(e)) }));
        });
    }

    [Test]
    public void TicketDigest_ListsStagesAsBulletLists()
    {
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.FromHours(8));
        Ticket[] tickets =
        [
            new(12, "open", "MacBook", "屏幕闪", null, now.AddDays(-5), now.AddDays(-3)),
            new(13, "committed", null, "蓝屏", "李四", now.AddDays(-5), now.AddHours(-30)),
        ];

        var post = TicketDigest.ToPost(tickets, now, "old");

        Assert.Multiple(() =>
        {
            Assert.That(
                post.Markdown,
                Is.EqualTo(
                    "🛠 **2 张工单卡住了**\n\n**待接单**\n• #12 MacBook · 屏幕闪 · 已 3 天\n\n**待审核**\n• #13 蓝屏 · 李四 · 已 1 天"
                )
            );
            Assert.That(post.Tags, Is.EqualTo(new[] { "维修", "提醒" }));
            Assert.That(post.Links.Single().Url, Is.EqualTo(TicketDigest.PortalUrl));
        });
    }

    [Test]
    public void MinecraftChat_BoldsPlayerAndIsNotSilent()
    {
        var post = MinecraftPosts.Chat("Steve_1", "hi *all*", "old");

        Assert.Multiple(() =>
        {
            Assert.That(post.Markdown, Is.EqualTo("**Steve\\_1**: hi \\*all\\*"));
            Assert.That(post.Silent, Is.False);
            Assert.That(post.Tags, Is.Empty);
        });
    }

    [TestCase("join", "➕ Steve 加入了服务器")]
    [TestCase("quit", "➖ Steve 离开了服务器")]
    public void MinecraftJoinQuit_AreSilent(string kind, string markdown)
    {
        var post = kind == "join" ? MinecraftPosts.Join("Steve", "old") : MinecraftPosts.Quit("Steve", "old");

        Assert.Multiple(() =>
        {
            Assert.That(post.Markdown, Is.EqualTo(markdown));
            Assert.That(post.Silent, Is.True);
        });
    }

    [Test]
    public void MinecraftDeathAndAdvancement_AreSilent()
    {
        var death = MinecraftPosts.Death("Steve was slain", "old");
        var advancement = MinecraftPosts.Advancement("Steve", "Stone Age", "Mine stone", "old");

        Assert.Multiple(() =>
        {
            Assert.That(death.Markdown, Is.EqualTo("💀 Steve was slain"));
            Assert.That(advancement.Markdown, Is.EqualTo("🏆 **Steve** 完成了进度 **Stone Age**\n*Mine stone*"));
            Assert.That(death.Silent && advancement.Silent, Is.True);
        });
    }
}
