using HuaJiBot.NET.Adapter.Red.Message;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Logger;

namespace HuaJiBot.NET.Adapter.Red;

public class RedProtocolAdapter : BotServiceBase
{
    public RedProtocolAdapter(string url, string token)
    {
        _connector = new(this, url, token);
    }

    public override required ILogger Logger { get; init; }
    readonly Connector _connector;

    public override void Reconnect()
    {
        _ = _connector.Connect();
    }

    public override Task SetupServiceAsync()
    {
        return _connector.Connect();
    }

    public override string[] GetAllRobots()
    {
        throw new NotImplementedException();
    }

    public override void SendGroupMessage(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] message
    )
    {
        var msg = new MessageBuilder().SetTarget(targetGroup, ChatTypes.GroupMessage);
        foreach (var item in message)
        {
            switch (item)
            {
                case TextMessage text:
                    msg.AddText(text.Text);
                    break;
                case ImageMessage image:
                    msg.AddPic(image.ImagePath);
                    break;
                case AtMessage at:
                    msg.AddAt(at.Target);
                    break;
                case ReplyMessage reply:
                    msg.AddReply(reply.ReplayMsgSeq, reply.ReplyMsgId, reply.Target);
                    break;
            }
        }
        _ = _connector.Send(msg.Build());
    }

    public override void FeedbackAt(string? robotId, string targetGroup, string userId, string text)
    {
        SendGroupMessageInternal(targetGroup, text, userId);
    }

    public void SendGroupMessageInternal(
        string targetGroup,
        string message,
        string atNtUid = ""
    //, string atUid = ""
    )
    {
        var msg = new MessageBuilder().SetTarget(targetGroup, ChatTypes.GroupMessage);
        if (!string.IsNullOrWhiteSpace(atNtUid))
            msg.AddAt(atNtUid);
        msg.AddText(message);
        _ = _connector.Send(msg.Build());
        /*
      {
  "type": "message::send",
  "payload": {
    "msgId": "0",
    "elements": [
      {
        "elementId": "",
        "elementType": 1,
        "TextElement": {
          "content": "。",
          "atUid": "",
          "atNtUid": "",
          "atTinyUid": "",
          "atType": 0
        }
      }
    ],
    "peer": { "chatType": 2, "guildId": "", "peerUid": "626872357" }
  }
}
*/
        //var arr = new JArray();
        //var hasAt = !string.IsNullOrWhiteSpace(atUid);
        //if (hasAt)
        //{
        //    arr.Add(
        //        new JObject
        //        {
        //            ["elementId"] = "",
        //            ["elementType"] = 1,
        //            ["TextElement"] = new JObject
        //            {
        //                ["content"] = "@atUid",
        //                ["atUid"] = atUid,
        //                ["atNtUid"] = atNtUid,
        //                ["atTinyUid"] = "",
        //                ["atType"] = 2
        //            }
        //        }
        //    );
        //}
        //arr.Add(
        //    new JObject
        //    {
        //        ["elementId"] = "",
        //        ["elementType"] = 1,
        //        ["TextElement"] = new JObject
        //        {
        //            ["content"] = (hasAt ? " " : "") + message,
        //            ["atUid"] = "",
        //            ["atNtUid"] = "",
        //            ["atTinyUid"] = "",
        //            ["atType"] = 0
        //        }
        //    }
        //);
        //var obj = new JObject
        //{
        //    ["type"] = "message::send",
        //    ["payload"] = new JObject
        //    {
        //        ["msgId"] = "0",
        //        ["elements"] = arr,
        //        ["peer"] = new JObject()
        //        {
        //            ["chatType"] = 2,
        //            ["guildId"] = "",
        //            ["peerUid"] = targetGroup
        //        }
        //    }
        //};
        //_connector.Send(obj.ToString(Formatting.None));
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 取插件数据目录
    /// </summary>
    /// <returns></returns>
    public override string GetPluginDataPath()
    {
        var path = Path.GetFullPath(Path.Combine("plugins", "data")); //插件数据目录，当前目录下的plugins/data
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path); //自动创建目录
        return path;
    }
}
