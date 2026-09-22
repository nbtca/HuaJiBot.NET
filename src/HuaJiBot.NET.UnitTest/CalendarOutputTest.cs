using HuaJiBot.NET.Plugin.Calendar;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace HuaJiBot.NET.UnitTest;

internal class CalendarOutputTest
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 12, 0, 0, TimeSpan.FromHours(8));

    private const string Description =
        "NWDC - NBTCAwide Developers Conference 是由浙大宁波理工学院计算机协会定期举办的技术分享活动。\n"
        + "面向在校学生及已毕业从业者，围绕真实项目与技术实践进行交流与分享。\n\n"
        + "点击链接入会，或添加至会议列表：\nhttps://meet.lk.nbtca.space:10086/nwdc";

    private static Ical.Net.Calendar Calendar()
    {
        var calendar = new Ical.Net.Calendar();
        foreach (var (summary, hours, description) in new[]
        {
            ("确认工牌形式等相关内容", 31, (string?)null),
            ("NWDC （待定）", 80, Description),
        })
        {
            var start = Now.AddHours(hours);
            calendar.Events.Add(
                new CalendarEvent
                {
                    Summary = summary,
                    Description = description,
                    Start = new CalDateTime(start.UtcDateTime, "UTC"),
                    End = new CalDateTime(start.AddHours(2).UtcDateTime, "UTC"),
                }
            );
        }
        return calendar;
    }

    [Test]
    public void List_ShowsWholeLinkInsteadOfTruncatedDescription()
    {
        var output = Calendar().GetEvents(Now, Now.AddDays(7)).BuildTextOutput(Now);
        TestContext.Out.WriteLine(output);

        Assert.That(output, Does.Contain("    链接：https://meet.lk.nbtca.space:10086/nwdc"));
        Assert.That(output, Does.Not.Contain("描述"));
        Assert.That(output, Does.Not.Contain("..."));
        Assert.That(output, Does.Not.EndWith("\n"));
        Assert.That(output, Does.Not.Contain("\n\n\n"));
    }

    [Test]
    public void Detail_ShowsFullDescription()
    {
        var output = Calendar().GetEvents(Now.AddDays(3), Now.AddDays(4)).First().BuildTextOutput(Now);
        TestContext.Out.WriteLine(output);

        Assert.That(output, Does.Contain("描述：NWDC"));
        Assert.That(output, Does.EndWith("https://meet.lk.nbtca.space:10086/nwdc"));
    }
}
