using System.Text.Json;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.UnitTest;

internal sealed class NaturalLanguageCommandPlugin : PluginBase
{
    public readonly List<string?> ScheduleCalls = [];
    public readonly List<(TestEvent Type, string? Enabled)> EventCalls = [];

    [Command("日程", "查看近期日程", Alias = "schedule")]
    public void Schedule([CommandArgumentString("时间")] string? weeks, GroupMessageEventArgs e)
    {
        ScheduleCalls.Add(weeks);
    }

    [Command("事件", "开关事件", Alias = "events")]
    public void Events(
        [CommandArgumentEnum<TestEvent>("类型")] TestEvent? type,
        [CommandArgumentString("状态")] string? enabled,
        GroupMessageEventArgs e
    )
    {
        if (type is { } selected)
            EventCalls.Add((selected, enabled));
    }

    protected override void Unload() { }
}

public enum TestEvent
{
    [CommandEnumItem("死亡", "玩家死亡")]
    Death,
}

internal sealed class NaturalLanguageCommandTest
{
    private readonly RecordingAdapter _adapter = new();

    private GroupMessageEventArgs Message()
    {
        return new(() => new DefaultCommandReader([]), () => ValueTask.FromResult("group"))
        {
            Service = _adapter,
            RobotId = "bot",
            MessageId = "message-1",
            GroupId = "group-1",
            SenderId = "user-1",
            SenderMemberCard = "User",
            TextMessageLazy = new(() => string.Empty),
        };
    }

    [Test]
    public async Task StructuredCommandArgumentsUseParameterNamesAndEnumAliases()
    {
        var plugin = new NaturalLanguageCommandPlugin();
        var service = new CommandService(_adapter);
        service.SetupCommands(plugin);

        await service.ExecuteCommandAsync(
            "schedule",
            JsonDocument.Parse("""{"weeks":2}""").RootElement.Clone(),
            Message()
        );
        await service.ExecuteCommandAsync(
            "events",
            JsonDocument.Parse("""{"type":"死亡","enabled":"false"}""").RootElement.Clone(),
            Message()
        );

        Assert.That(plugin.ScheduleCalls, Is.EqualTo(new[] { "2" }));
        Assert.That(plugin.EventCalls, Is.EqualTo(new[] { (TestEvent.Death, "false") }));
    }

    [Test]
    public void CommandCatalogExposesPoliciesAndExamples()
    {
        var plugin = new NaturalLanguageCommandPlugin();
        var service = new CommandService(_adapter);
        service.SetupCommands(plugin);

        var catalog = service.GetCommandCatalog();

        Assert.That(catalog.Select(x => x.Id), Does.Contain("schedule"));
        Assert.That(catalog.Select(x => x.Id), Does.Contain("events"));
        Assert.That(catalog.Single(x => x.Id == "events").AdminOnly, Is.True);
        Assert.That(catalog.Single(x => x.Id == "schedule").Examples, Is.Not.Empty);
        Assert.That(catalog.Single(x => x.Id == "events").Parameters.Select(x => x.Name), Is.EqualTo(new[] { "type", "enabled", "e" }));
    }
}