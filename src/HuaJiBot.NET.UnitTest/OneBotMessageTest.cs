using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.UnitTest;

internal class OneBotMessageTest
{
    [Test]
    public void Deserialize_UnknownSegment_KeepsTheRestOfTheMessage()
    {
        var json = JArray.Parse(
            """
            [
              {"type":"mface","data":{"summary":"[doge]","emoji_id":"1"}},
              {"type":"file","data":{"file":"a.zip"}},
              {"type":"text","data":{"text":"hi"}}
            ]
            """
        );

        var message = json.ToObject<List<MessageEntity>>()!;

        Assert.Multiple(() =>
        {
            Assert.That(message, Has.Count.EqualTo(3));
            Assert.That(message[0], Is.TypeOf<UnknownMessageEntity>());
            Assert.That(message[2], Is.TypeOf<TextMessageEntity>());
        });
    }

    [Test]
    public void Serialize_At_KeepsLargeQqNumber()
    {
        var json = JObject.Parse(JsonConvert.SerializeObject(new AtMessageEntity("4294967296")));
        Assert.That(json["data"]!["qq"]!.Value<string>(), Is.EqualTo("4294967296"));
    }
}
