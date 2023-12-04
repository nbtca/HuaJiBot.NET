using System.ComponentModel;
using System.Text.RegularExpressions;
using HuaJiBot.NET.Commands;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.RepairTeam;

public class PluginMain : PluginBase
{
    protected override Task Initialize()
    {
        Service.Log("[1] 启动成功！");
        return Task.CompletedTask;
    }

    [CommandEnum("test")]
    enum TEST
    {
        [CommandEnumItem("")]
        s,

        [CommandEnumItem("")]
        a
    }

    [Commands.Command("test")]
    private void TestCommand(
        [CommandArgumentString("test")] string a,
        [CommandArgumentEnum<TEST>("test")] TEST b
    ) { }

    protected override void Unload() { }
}
