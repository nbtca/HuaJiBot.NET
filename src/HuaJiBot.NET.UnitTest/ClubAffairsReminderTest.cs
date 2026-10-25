using HuaJiBot.NET.Plugin.Calendar;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using FilterMode = HuaJiBot.NET.Plugin.Calendar.PluginConfig.ReminderFilterConfig.FilterMode;

namespace HuaJiBot.NET.UnitTest;

internal class ClubAffairsReminderTest
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 9, 0, 0, TimeSpan.FromHours(8));

    private static CalendarEvent Event(
        string summary,
        DateTimeOffset start,
        DateTimeOffset end,
        string? description = null
    ) =>
        new()
        {
            Summary = summary,
            Description = description,
            Start = new CalDateTime(start.UtcDateTime, "UTC"),
            End = new CalDateTime(end.UtcDateTime, "UTC"),
        };

    private static (RecordingAdapter adapter, ClubAffairsReminder reminder) Setup()
    {
        var calendar = new Ical.Net.Calendar();
        // Starts tomorrow at 10:00.
        calendar.Events.Add(Event("招新宣讲", Now.AddHours(25), Now.AddHours(27)));
        // Started yesterday and runs until the day after tomorrow.
        calendar.Events.Add(Event("例会筹备", Now.AddDays(-1), Now.AddDays(2)));
        var config = new PluginConfig
        {
            ReminderGroups =
            [
                new() { GroupId = "white", Mode = FilterMode.WhiteList, Keywords = ["招新"] },
                new() { GroupId = "all", Mode = FilterMode.BlackList, Keywords = [] },
                new() { GroupId = "none", Mode = FilterMode.WhiteList, Keywords = ["不存在"] },
            ],
        };
        var adapter = new RecordingAdapter();
        return (adapter, new ClubAffairsReminder(adapter, config, () => calendar));
    }

    private static string SentTo(RecordingAdapter adapter, string group) =>
        string.Concat(
            adapter.Sends.Where(x => x.Target == group).SelectMany(x => x.Messages).OfType<Bot.TextMessage>().Select(x => x.Text)
        );

    [Test]
    public void WeeklySummary_PostsTaggedListOnTelegram()
    {
        var (adapter, reminder) = Setup();

        reminder.SendWeeklySummary(Now);

        var post = adapter.Posts.First(x => x.Markdown.Contains("招新宣讲") && !x.Markdown.Contains("例会筹备"));
        Assert.Multiple(() =>
        {
            Assert.That(post.Markdown, Is.EqualTo("📢 **社团事务一周预告**（09月21日～09月28日）\n\n- 09月22日 **招新宣讲**"));
            Assert.That(post.Tags, Is.EqualTo(new[] { "日程", "周预告" }));
        });
    }

    [Test]
    public void WeeklySummary_SendsEachGroupOnlyItsOwnEvents()
    {
        var (adapter, reminder) = Setup();

        Assert.That(reminder.SendWeeklySummary(Now), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(SentTo(adapter, "white"), Does.Contain("招新宣讲").And.Not.Contain("例会筹备"));
            Assert.That(SentTo(adapter, "all"), Does.Contain("招新宣讲").And.Contain("例会筹备"));
            Assert.That(adapter.Sends.Select(x => x.Target), Does.Not.Contain("none"));
        });
    }

    [Test]
    public void DailyReminder_SkipsEventsThatStartedEarlier()
    {
        var (adapter, reminder) = Setup();

        Assert.That(reminder.SendDailyReminder(Now), Is.True);

        Assert.That(SentTo(adapter, "all"), Does.Contain("招新宣讲").And.Not.Contain("例会筹备"));
    }

    [Test]
    public void Reminders_SkipEventsMarkedWithSkipNotification()
    {
        var calendar = new Ical.Net.Calendar();
        calendar.Events.Add(
            Event(
                "NWDC （待定）",
                Now.AddHours(25),
                Now.AddHours(27),
                "具体日期待确认\n[skip notification]"
            )
        );
        calendar.Events.Add(Event("正式活动", Now.AddHours(26), Now.AddHours(28)));
        var config = new PluginConfig
        {
            ReminderGroups = [new() { GroupId = "all", Mode = FilterMode.BlackList, Keywords = [] }],
        };
        var adapter = new RecordingAdapter();
        var reminder = new ClubAffairsReminder(adapter, config, () => calendar);

        Assert.Multiple(() =>
        {
            Assert.That(reminder.SendWeeklySummary(Now), Is.True);
            Assert.That(reminder.SendDailyReminder(Now), Is.True);
            Assert.That(SentTo(adapter, "all"), Does.Contain("正式活动").And.Not.Contain("NWDC"));
        });
    }

    [TestCase("[skip notification]")]
    [TestCase("Details\n[SKIP NOTIFICATION]\n")]
    public void SkipNotificationKeyword_IsCaseInsensitive(string description)
    {
        var calendarEvent = Event("Placeholder", Now, Now.AddHours(1), description);

        Assert.That(calendarEvent.ShouldSkipNotification(), Is.True);
    }

    [Test]
    public void SkipNotificationKeyword_DoesNotHideOtherEvents()
    {
        var calendarEvent = Event("Real event", Now, Now.AddHours(1), "No notification marker");

        Assert.That(calendarEvent.ShouldSkipNotification(), Is.False);
    }

    [Test]
    public void Reminders_WithoutCalendar_AreRetriedLater()
    {
        var adapter = new RecordingAdapter();
        var reminder = new ClubAffairsReminder(adapter, new PluginConfig(), () => null);

        Assert.Multiple(() =>
        {
            Assert.That(reminder.SendWeeklySummary(Now), Is.False);
            Assert.That(reminder.SendDailyReminder(Now), Is.False);
        });
    }

    [Test]
    public void MarkdownRender_NullBody_RendersNothing()
    {
        Assert.That(Utils.CardBuilder.MarkdownRender(null), Is.Empty);
    }
}
