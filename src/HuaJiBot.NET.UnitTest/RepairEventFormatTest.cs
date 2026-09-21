using HuaJiBot.NET.Plugin.RepairTeam;
using Newtonsoft.Json;

namespace HuaJiBot.NET.UnitTest;

internal class RepairEventFormatTest
{
    private static string Format(string json) =>
        PluginMain.FormatEvent(JsonConvert.DeserializeObject<PluginMain.LogEventEntity>(json)!);

    [Test]
    public void Accept_ShowsChineseActionMemberAndLocalTime()
    {
        var text = Format(
            """
            {"event_id":900001,"action":"accept","member_id":"2333333333","member_alias":"测试队员",
             "gmt_create":"2026-09-21T03:45:53Z","model":"测试机型","problem":"无法开机","description":"已接单"}
            """
        );

        Assert.That(
            text,
            Is.EqualTo(
                """
                【维修 #900001】已接单
                测试队员 · 09-21 11:45
                机型：测试机型
                问题：无法开机
                说明：已接单
                """.ReplaceLineEndings()
            )
        );
    }

    [Test]
    public void Create_WithoutMemberOrDescription_ShowsOnlyTime()
    {
        var text = Format(
            """{"event_id":1,"action":"create","member_id":"","member_alias":"","gmt_create":"2026-09-21T11:45:00+08:00","model":"","problem":"蓝屏"}"""
        );

        Assert.That(text, Is.EqualTo("【维修 #1】新报修\n09-21 11:45\n问题：蓝屏".ReplaceLineEndings()));
    }

    [Test]
    public void UnknownAction_IsShownAsIs()
    {
        var text = Format("""{"event_id":1,"action":"archive","gmt_create":"2026-09-21T11:45:00+08:00"}""");
        Assert.That(text, Does.StartWith("【维修 #1】archive"));
    }
}
