using HuaJiBot.NET.Adapter.Red;
using HuaJiBot.NET.Bot;
using Newtonsoft.Json;

namespace HuaJiBot.NET.UnitTest;

internal class TestAdapter : BotServiceBase
{
    public override Task SetupServiceAsync()
    {
        return Task.CompletedTask;
    }

    public override string[] GetAllRobots()
    {
        throw new NotImplementedException();
    }

    public override void SendGroupMessage(
        string? robotId,
        string targetGroup,
        params SendingMessageBase[] messages
    )
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string? robotId, string targetGroup, string userId, string text)
    {
        throw new NotImplementedException();
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    public override void Log(object message)
    {
        throw new NotImplementedException();
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

public class Tests
{
    [SetUp]
    public void Setup() { }

    private readonly TestAdapter api = new();

    [Test]
    public void Test1()
    {
        string inputCommand = "test aaaa '测试  内容' testa";
        var raw = $$"""
                  {
                    "msgId": "7299839010006944912",
                    "msgRandom": "636421449",
                    "msgSeq": "76022",
                    "cntSeq": "0",
                    "chatType": 2,
                    "msgType": 2,
                    "subMsgType": 1,
                    "sendType": 0,
                    "peerUid": "632687257",
                    "channelId": "",
                    "guildId": "",
                    "guildCode": "0",
                    "fromUid": "0",
                    "fromAppid": "0",
                    "msgTime": "1635995439",
                    "msgMeta": "0x",
                    "sendStatus": 2,
                    "sendMemberName": "Tteser",
                    "sendNickName": "",
                    "guildName": "",
                    "channelName": "",
                    "elements": [
                      {
                        "elementType": 1,
                        "elementId": "7293901944912980004",
                        "extBufForUI": "0x",
                        "textElement": {
                          "content": "@hsin",
                          "atType": 2,
                          "atUid": "0",
                          "atTinyId": "0",
                          "atNtUid": "u_eFauRHKP3ZwWVC48wd11SQ",
                          "subElementType": 0,
                          "atChannelId": "0",
                          "atRoleId": "0",
                          "atRoleColor": 0,
                          "atRoleName": "",
                          "needNotify": 0
                        },
                        "faceElement": null,
                        "marketFaceElement": null,
                        "replyElement": null,
                        "picElement": null,
                        "pttElement": null,
                        "videoElement": null,
                        "grayTipElement": null,
                        "arkElement": null,
                        "fileElement": null,
                        "liveGiftElement": null,
                        "markdownElement": null,
                        "structLongMsgElement": null,
                        "multiForwardMsgElement": null,
                        "giphyElement": null,
                        "walletElement": null,
                        "inlineKeyboardElement": null,
                        "textGiftElement": null,
                        "calendarElement": null,
                        "yoloGameResultElement": null,
                        "avRecordElement": null
                      },
                      {
                        "elementType": 1,
                        "elementId": "7299010044912983905",
                        "extBufForUI": "0x",
                        "textElement": {
                          "content": "{{inputCommand}}",
                          "atType": 0,
                          "atUid": "0",
                          "atTinyId": "0",
                          "atNtUid": "",
                          "subElementType": 0,
                          "atChannelId": "0",
                          "atRoleId": "0",
                          "atRoleColor": 0,
                          "atRoleName": "",
                          "needNotify": 0
                        },
                        "faceElement": null,
                        "marketFaceElement": null,
                        "replyElement": null,
                        "picElement": null,
                        "pttElement": null,
                        "videoElement": null,
                        "grayTipElement": null,
                        "arkElement": null,
                        "fileElement": null,
                        "liveGiftElement": null,
                        "markdownElement": null,
                        "structLongMsgElement": null,
                        "multiForwardMsgElement": null,
                        "giphyElement": null,
                        "walletElement": null,
                        "inlineKeyboardElement": null,
                        "textGiftElement": null,
                        "calendarElement": null,
                        "yoloGameResultElement": null,
                        "avRecordElement": null
                      }
                    ],
                    "records": [],
                    "emojiLikesList": [],
                    "commentCnt": "0",
                    "directMsgFlag": 0,
                    "directMsgMembers": [],
                    "peerName": "垃圾桶",
                    "freqLimitInfo": null,
                    "editable": false,
                    "avatarMeta": "",
                    "avatarPendant": "",
                    "feedId": "",
                    "roleId": "0",
                    "timeStamp": "0",
                    "clientIdentityInfo": null,
                    "isImportMsg": false,
                    "atType": 2,
                    "fromChannelRoleInfo": {
                      "roleId": "0",
                      "name": "",
                      "color": 0
                    },
                    "fromGuildRoleInfo": {
                      "roleId": "0",
                      "name": "",
                      "color": 0
                    },
                    "levelRoleInfo": {
                      "roleId": "0",
                      "name": "",
                      "color": 0
                    },
                    "recallTime": "0",
                    "isOnlineMsg": true,
                    "generalFlags": "0x",
                    "clientSeq": "0",
                    "fileGroupSize": null,
                    "foldingInfo": null,
                    "nameType": 0,
                    "avatarFlag": 0,
                    "anonymousExtInfo": null,
                    "personalMedal": null,
                    "roleManagementTag": null,
                    "senderUin": "494841870",
                    "peerUin": "635268727"
                  }
                  
                  """;
        var redCommandReader = new RedCommandReader(
            api,
            JsonConvert.DeserializeObject<MessageRecv>(raw)!
        );
        {
            var result = redCommandReader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = redCommandReader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = redCommandReader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = redCommandReader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = redCommandReader.Match(new[] { "test", }, x => x, out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
        {
            var result = redCommandReader.Input(out var test);
            Console.WriteLine(result);
            Console.WriteLine(test);
        }
    }
}
