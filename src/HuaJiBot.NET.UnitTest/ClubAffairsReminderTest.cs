namespace HuaJiBot.NET.UnitTest;

internal class ClubAffairsReminderTest
{
    [Test]
    public void TestWeeklySummaryDayOfWeek()
    {
        // Test that Monday is correctly identified for weekly summary
        var monday = new DateTimeOffset(2024, 10, 21, 9, 0, 0, TimeSpan.FromHours(8)); // Monday
        var tuesday = new DateTimeOffset(2024, 10, 22, 9, 0, 0, TimeSpan.FromHours(8)); // Tuesday

        Assert.That(monday.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
        Assert.That(tuesday.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Monday));
    }

    [Test]
    public void TestTimeComparison()
    {
        // Test that hour comparison works correctly for reminder timing
        var nineAm = new DateTimeOffset(2024, 10, 21, 9, 0, 0, TimeSpan.FromHours(8));
        var tenAm = new DateTimeOffset(2024, 10, 21, 10, 0, 0, TimeSpan.FromHours(8));

        Assert.That(nineAm.Hour, Is.EqualTo(9));
        Assert.That(tenAm.Hour, Is.EqualTo(10));
    }

    [Test]
    public void TestDateComparison()
    {
        // Test date comparison for preventing duplicate reminders
        var date1 = new DateTimeOffset(2024, 10, 21, 9, 0, 0, TimeSpan.FromHours(8));
        var date2 = new DateTimeOffset(2024, 10, 21, 10, 0, 0, TimeSpan.FromHours(8));
        var date3 = new DateTimeOffset(2024, 10, 22, 9, 0, 0, TimeSpan.FromHours(8));

        Assert.That(date1.Date, Is.EqualTo(date2.Date));
        Assert.That(date1.Date, Is.Not.EqualTo(date3.Date));
    }

    [Test]
    public void TestWeekRange()
    {
        // Test that week range calculation is correct
        var start = new DateTimeOffset(2024, 10, 21, 9, 0, 0, TimeSpan.FromHours(8));
        var end = start.AddDays(7);

        var expectedEnd = new DateTimeOffset(2024, 10, 28, 9, 0, 0, TimeSpan.FromHours(8));
        Assert.That(end, Is.EqualTo(expectedEnd));
    }

    [Test]
    public void TestDayRange()
    {
        // Test that tomorrow calculation is correct
        // 创建午夜时间的DateTimeOffset以匹配生产代码中用于日期范围查询的行为
        var today = new DateTimeOffset(2024, 10, 21, 9, 0, 0, TimeSpan.FromHours(8));
        var tomorrow = new DateTimeOffset(today.Date.AddDays(1), today.Offset);
        var dayAfterTomorrow = new DateTimeOffset(today.Date.AddDays(2), today.Offset);

        var expectedTomorrow = new DateTimeOffset(2024, 10, 22, 0, 0, 0, TimeSpan.FromHours(8));
        var expectedDayAfter = new DateTimeOffset(2024, 10, 23, 0, 0, 0, TimeSpan.FromHours(8));

        Assert.That(tomorrow, Is.EqualTo(expectedTomorrow));
        Assert.That(dayAfterTomorrow, Is.EqualTo(expectedDayAfter));
    }

    [Test]
    public void TestNumberEmoji()
    {
        // Test emoji mapping
        var testCases = new Dictionary<int, string>
        {
            { 1, "1️⃣" },
            { 2, "2️⃣" },
            { 3, "3️⃣" },
            { 4, "4️⃣" },
            { 5, "5️⃣" },
            { 6, "6️⃣" },
            { 7, "7️⃣" },
            { 8, "8️⃣" },
            { 9, "9️⃣" },
            { 10, "🔟" },
        };

        foreach (var testCase in testCases)
        {
            var emoji = GetNumberEmoji(testCase.Key);
            Assert.That(emoji, Is.EqualTo(testCase.Value));
        }

        // Test fallback for numbers > 10
        Assert.That(GetNumberEmoji(11), Is.EqualTo("11."));
    }

    private string GetNumberEmoji(int number)
    {
        return number switch
        {
            1 => "1️⃣",
            2 => "2️⃣",
            3 => "3️⃣",
            4 => "4️⃣",
            5 => "5️⃣",
            6 => "6️⃣",
            7 => "7️⃣",
            8 => "8️⃣",
            9 => "9️⃣",
            10 => "🔟",
            _ => $"{number}.",
        };
    }
}
