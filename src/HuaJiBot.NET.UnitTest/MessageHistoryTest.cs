using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Interfaces;
using Moq;

namespace HuaJiBot.NET.UnitTest;

internal class MessageHistoryTest
{
    [Test]
    public void GetGroupMessagesByTimeRange_OnlyReturnsMessagesFromRequestedGroupAndDay()
    {
        var dataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var service = new Mock<IPluginService>();
        service.Setup(x => x.GetPluginDataPath()).Returns(dataPath);

        try
        {
            using var history = new MessageHistory(service.Object, "messages.db");
            var yesterday = new DateTime(2026, 9, 5);
            history.StoreMessage(CreateMessage("group-a-yesterday", "group-a", yesterday.AddHours(12)));
            history.StoreMessage(CreateMessage("group-b-yesterday", "group-b", yesterday.AddHours(12)));
            history.StoreMessage(CreateMessage("group-a-today", "group-a", yesterday.AddDays(1)));

            var messages = history
                .GetGroupMessagesByTimeRange("group-a", yesterday, yesterday.AddDays(1))
                .ToList();

            Assert.That(messages.Select(message => message.MessageId), Is.EqualTo(["group-a-yesterday"]));
            Assert.That(history.GetGroupIds(), Is.EquivalentTo(["group-a", "group-b"]));
        }
        finally
        {
            if (Directory.Exists(dataPath))
                Directory.Delete(dataPath, true);
        }
    }

    private static GroupMessage CreateMessage(string messageId, string groupId, DateTime timestamp)
    {
        return new GroupMessage
        {
            MessageId = messageId,
            GroupId = groupId,
            SenderId = "sender",
            SenderName = "Sender",
            Content = "message",
            IsBot = false,
            ReplyToMessageId = null,
            Timestamp = timestamp,
        };
    }
}
