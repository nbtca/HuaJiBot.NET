using HuaJiBot.NET.Adapter.Red.Message;
using HuaJiBot.NET.Bot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.Red;

public class RedProtocolAdapter : BotServiceBase
{
    public RedProtocolAdapter(string url, string token)
    {
        _connector = new(this, url, token);
    }

    readonly Connector _connector;

    public override async Task SetupService()
    {
        await _connector.Connect();
    }

    public override string[] GetAllRobots()
    {
        throw new NotImplementedException();
    }

    public override void SendGroupMessage(string? robotId, string targetGroup, string message)
    {
        SendGroupMessageInternal(targetGroup, message);
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
    /// 日志
    /// </summary>
    /// <param name="message">日志内容</param>
    public override void Log(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}");
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    /// <param name="message"></param>
    public override void Warn(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}");
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    /// <param name="message">调试日志内容</param>
    public override void LogDebug(object message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] {message}");
    }

    /// <summary>
    /// 错误日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="detail">错误信息</param>
    public override void LogError(object message, object detail)
    {
        Console.WriteLine(
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}"
                + $"{Environment.NewLine}---{Environment.NewLine}"
                + $"{detail}"
                + $"{Environment.NewLine}---"
        );
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
