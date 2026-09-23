using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.UnitTest;

internal class CommandMatchTest
{
    private class SchedulePlugin : PluginBase
    {
        public readonly List<string?> Calls = [];

        [Command("日程", "")]
        public void Schedule([CommandArgumentString("时间")] string? time) => Calls.Add(time);

        protected override void Unload() { }
    }

    private readonly RecordingAdapter _adapter = new();

    private GroupMessageEventArgs Message(string text, RecordingAdapter? adapter = null) =>
        new(() => new DefaultCommandReader([text]), () => ValueTask.FromResult("group"))
        {
            Service = adapter ?? _adapter,
            MessageId = "1",
            GroupId = "group",
            SenderId = "user",
            SenderMemberCard = "user",
            TextMessageLazy = new(() => text),
        };

    [TestCase("日程", null)]
    [TestCase("日程 2", "2")]
    [TestCase("日程\u30002", "2")]
    public void Command_MatchesWholeWord(string text, string? expected)
    {
        var plugin = new SchedulePlugin();
        var service = new CommandService(_adapter);
        service.SetupCommands(plugin);

        service.ProcessCommand(Message(text));

        Assert.That(plugin.Calls, Is.EqualTo(new[] { expected }));
    }

    [TestCase("日程安排好了吗")]
    [TestCase("日程2")]
    public void Command_IgnoresSentencesStartingWithCommand(string text)
    {
        var plugin = new SchedulePlugin();
        var service = new CommandService(_adapter);
        service.SetupCommands(plugin);

        service.ProcessCommand(Message(text));

        Assert.That(plugin.Calls, Is.Empty);
    }

    [TestCase("帮助", true)]
    [TestCase("help", true)]
    [TestCase("帮助我看看这个报错", false)]
    public void Help_MatchesWholeWord(string text, bool expected)
    {
        var service = new CommandService(_adapter);
        Assert.That(service.ProcessHelp(Message(text)), Is.EqualTo(expected));
    }

    [Test]
    public async Task Help_OnQq_RepliesWithTheSameText()
    {
        RecordingAdapter adapter = new();
        var service = new CommandService(adapter);
        service.SetupCommands(new SchedulePlugin());

        service.ProcessHelp(Message("帮助", adapter));
        for (var i = 0; i < 50 && adapter.Sends.Count == 0; i++)
            await Task.Delay(20);

        Assert.That(
            adapter.Sends.Single().Messages,
            Is.EqualTo(
                new SendingMessageBase[]
                {
                    new ReplyMessage("1"),
                    new TextMessage($"可用命令：{Environment.NewLine}日程 <string> {Environment.NewLine}    {Environment.NewLine}"),
                }
            )
        );
    }

    [Test]
    public void RestOfMessage_WithReplyInTheMiddle_HasNoRecordText()
    {
        var reader = new DefaultCommandReader(
            ["看看 ", new CommonCommandReader.ReaderReply(new(messageId: "1")), "这条", new CommonCommandReader.ReaderAt("10001")]
        );

        Assert.That(reader.Input(out var text, true), Is.True);
        Assert.That(text, Is.EqualTo("看看 这条@10001 "));
    }
}
