using System.Text.Json;
using HuaJiBot.NET.Adapter.Telegram;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Logger;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HuaJiBot.NET.UnitTest;

internal class TelegramCommandTest
{
    private class SchedulePlugin : PluginBase
    {
        [Command("日程", "查看近期日程", Alias = "schedule")]
        public void Schedule([CommandArgumentString("周数")] string? weeks) { }

        protected override void Unload() { }
    }

    private static TelegramAdapter Adapter(TelegramRichMessageFallbackTest.FakeBotApi api)
    {
        var adapter = new TelegramAdapter("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", new(api))
        {
            Logger = new ConsoleLogger(),
            BotUser = new User { Id = 42, IsBot = true, FirstName = "NBoT", Username = "nbtca_bot" },
        };
        adapter.SetupCommands(new SchedulePlugin());
        return adapter;
    }

    private static Message Text(string text)
    {
        var first = text.Split(' ')[0];
        MessageEntity[]? entities = first switch
        {
            _ when first.StartsWith('/') && first[1..].All(char.IsAscii) =>
            [
                new() { Type = MessageEntityType.BotCommand, Offset = 0, Length = first.Length },
            ],
            _ when first.StartsWith('@') =>
            [
                new() { Type = MessageEntityType.Mention, Offset = 0, Length = first.Length },
            ],
            _ => null,
        };
        return new()
        {
            Text = text,
            Entities = entities,
            Chat = new() { Id = -100, Type = ChatType.Supergroup },
        };
    }

    private static CommandReader Reader(string text) =>
        new TelegramCommandReader(Adapter(new()), Text(text));

    [TestCase("/schedule 2")]
    [TestCase("/schedule@nbtca_bot 2")]
    [TestCase("/SCHEDULE@NBTCA_BOT 2")]
    [TestCase("/日程 2")]
    [TestCase("日程 2")]
    public void Command_ResolvesToItsName(string text)
    {
        var reader = Reader(text);

        Assert.Multiple(() =>
        {
            Assert.That(reader.Input(out var name), Is.True);
            Assert.That(name, Is.EqualTo("日程"));
            Assert.That(reader.Input(out var arg), Is.True);
            Assert.That(arg, Is.EqualTo("2"));
        });
    }

    [Test]
    public void Command_ForAnotherBot_IsIgnored() =>
        Assert.That(Reader("/schedule@other_bot 2").Input(out _), Is.False);

    [Test]
    public void Mention_OfThisBot_IsAnAt()
    {
        var reader = Reader("@nbtca_bot 你好");

        Assert.Multiple(() =>
        {
            Assert.That(reader.At(out var target), Is.True);
            Assert.That(target, Is.EqualTo("42"));
        });
    }

    [Test]
    public async Task SetupCommands_RegistersAliasesInTheMenu()
    {
        TelegramRichMessageFallbackTest.FakeBotApi api = new();

        Adapter(api);
        for (var i = 0; i < 50 && !api.Methods.Contains("setMyCommands"); i++)
            await Task.Delay(20);

        var commands =
            from x in JsonDocument
                .Parse(api.Bodies[api.Methods.LastIndexOf("setMyCommands")])
                .RootElement.GetProperty("commands")
                .EnumerateArray()
            select (x.GetProperty("command").GetString(), x.GetProperty("description").GetString());
        Assert.That(commands, Is.EqualTo(new[] { ("schedule", "查看近期日程"), ("help", "查看全部命令") }));
    }

    [Test]
    public async Task Help_OnTelegram_ListsAliases()
    {
        TelegramRichMessageFallbackTest.FakeBotApi api = new();
        var adapter = Adapter(api);
        var service = new CommandService(adapter);
        service.SetupCommands(new SchedulePlugin());

        service.ProcessHelp(
            new GroupMessageEventArgs(() => new DefaultCommandReader(["帮助"]), () => ValueTask.FromResult("group"))
            {
                Service = adapter,
                MessageId = "1",
                GroupId = "-100",
                SenderId = "user",
                SenderMemberCard = "user",
                TextMessageLazy = new(() => "帮助"),
            }
        );
        for (var i = 0; i < 50 && !api.Methods.Contains("sendMessage"); i++)
            await Task.Delay(20);

        var message = JsonDocument.Parse(api.Bodies[api.Methods.IndexOf("sendMessage")]).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(
                message.GetProperty("text").GetString(),
                Is.EqualTo("<b>可用命令</b>\n• /schedule &lt;周数&gt; — 查看近期日程\n• /help — 查看全部命令")
            );
            Assert.That(message.GetProperty("reply_parameters").GetProperty("message_id").GetInt32(), Is.EqualTo(1));
        });
    }
}
