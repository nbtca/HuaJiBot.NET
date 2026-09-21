using HuaJiBot.NET.Plugin.Calendar;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using FilterMode = HuaJiBot.NET.Plugin.Calendar.PluginConfig.ReminderFilterConfig.FilterMode;

namespace HuaJiBot.NET.UnitTest;

internal class ClubAffairsReminderTest
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 9, 0, 0, TimeSpan.FromHours(8));

    private static CalendarEvent Event(string summary, DateTimeOffset start, DateTimeOffset end) =>
        new()
        {
            Summary = summary,
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
