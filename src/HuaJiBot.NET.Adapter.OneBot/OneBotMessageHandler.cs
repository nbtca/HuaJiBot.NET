using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Adapter.OneBot;

internal class OneBotMessageHandler(OneBotApi api, BotServiceBase service)
{
    private string? _qq = null;
    public string? QQ => _qq;

    public async Task ProcessMessageAsync(string data)
    {
#if !DEBUG
        try
        {
#endif
        var json = JObject.Parse(data);
        if (json.ContainsKey("echo"))
        {
            await api.ProcessMessageAsync(json);
            return;
        }
        var postType = json.Value<string>("post_type");
        switch (postType)
        {
            case "meta_event":
                {
                    var metaEventType = json.Value<string>("meta_event_type");
                    switch (metaEventType)
                    {
                        //{"meta_event_type":"lifecycle",
                        //  "post_type":"meta_event",
                        //  "self_id":1,
                        //  "sub_type":"connect",
                        //  "time":1659587395
                        //  }
                        case "lifecycle":
                            if (json.Value<string>("sub_type") == "connect")
                            {
                                _qq = json.Value<string>("self_id");
                                service.Log("登录成功！帐号：" + _qq);
                                //{
                                //        "data": {
                                //        "app_full_name": "go-cqhttp-v1.0.0-rc3_windows_amd64-go1.18.3",
                                //        "app_name": "go-cqhttp",
                                //        "app_version": "v1.0.0-rc3",
                                //        "coolq_directory": "A:\\Documents\\GitHub\\**\\src\\\\bin\\Debug\\net462",
                                //        "coolq_edition": "pro",
                                //        "go-cqhttp": true,
                                //        "plugin_build_configuration": "release",
                                //        "plugin_build_number": 99,
                                //        "plugin_version": "4.15.0",
                                //        "protocol": 0,
                                //        "protocol_version": "v11",
                                //        "runtime_os": "windows",
                                //        "runtime_version": "go1.18.3",
                                //        "version": "v1.0.0-rc3"
                                //                                        },
                                //    "retcode": 0,
                                //    "status": "ok"
                                //}
                                //"data": {
                                //    "app_name": "Lagrange.OneBot",
                                //    "app_version": "0.0.3",
                                //    "protocol_version": "v11",
                                //    "nt_protocol": "Linux | 3.1.2-13107"
                                //},
                                var info = await api.GetVersionInfoAsync();
#if DEBUG
                                service.Log(
                                    "GetVersionInfoAsync:" + JsonConvert.SerializeObject(info)
                                );
#endif
                                var appName = info["app_name"];
                                var appVersion = info["app_version"];
                                if (info.TryGetValue("nt_protocol", out var protocolVersion))
                                {
                                    appName = appName + " v" + appVersion;
                                    appVersion = protocolVersion;
                                }
                                Events.Events.CallOnBotLogin(
                                    new BotLoginEventArgs
                                    {
                                        Accounts = _qq is null ? [] : [_qq],
                                        ClientName = appName,
                                        ClientVersion = appVersion,
                                        Service = service
                                    }
                                );
                            }
                            break;
                        //{"post_type":"meta_event","meta_event_type":"heartbeat","time":1659587845,"self_id":1,"status":{"app_enabled":true,"app_good":true,"app_initialized":true,"good":true,"online":true,"plugins_good":null,"stat":{"packet_received":75,"packet_sent":66,"packet_lost":0,"message_received":0,"message_sent":0,"last_message_time":0,"disconnect_times":0,"lost_times":0}},"interval":5000}
                        case "heartbeat":
                            break;
                        default:
#if DEBUG
                            service.LogDebug(json.ToString());
#endif
                            break;
                    }
                }
                break;
            case "message":
                {
                    var messageType = json.Value<string>("message_type");
                    switch (messageType)
                    {
                        case "group":
                            {
                                /*{
                                    "post_type": "message",
                                    "message_type": "group",
                                    "time": 1,
                                    "self_id": 1,
                                    "sub_type": "normal",
                                    "group_id": 1,
                                    "message": [
                                        {
                                            "type": "reply",
                                            "data": {
                                                "id": "2001268508"
                                            }
                                        },
                                        {
                                            "type": "at",
                                            "data": {
                                                "qq": "1"
                                            }
                                        },
                                        {
                                            "type": "text",
                                            "data": {
                                                "text": " "
                                            }
                                        },
                                        {
                                            "type": "image",
                                            "data": {
                                                "file": "9183ab6f146208a5f8f3835e02a4be6c.image",
                                                "subType": "0",
                                                "url": "https://gchat.qpic.cn/gchatpic_new/1/1-2551983170-9183AB6F146208A5F8F3835E02A4BE6C/0?term=3"
                                            }
                                        },
                                        {
                                            "type": "image",
                                            "data": {
                                                "file": "cee8c6bb7b1c5fc4be0a406aa2746c3b.image",
                                                "subType": "0",
                                                "url": "https://gchat.qpic.cn/gchatpic_new/1/1-2945018168-CEE8C6BB7B1C5FC4BE0A406AA2746C3B/0?term=3"
                                            }
                                        },
                                        {
                                            "type": "face",
                                            "data": {
                                                "id": "212"
                                            }
                                        }
                                    ],
                                    "user_id": 1,
                                    "anonymous": null,
                                    "font": 0,
                                    "message_seq": 26460,
                                    "raw_message": "[CQ:reply,id=2001268508][CQ:at,qq=1] [CQ:image,file=9183ab6f146208a5f8f3835e02a4be6c.image,subType=0,url=https://gchat.qpic.cn/gchatpic_new/1/1-2551983170-9183AB6F146208A5F8F3835E02A4BE6C/0?term=3][CQ:image,file=cee8c6bb7b1c5fc4be0a406aa2746c3b.image,subType=0,url=https://gchat.qpic.cn/gchatpic_new/1/1-2945018168-CEE8C6BB7B1C5FC4BE0A406AA2746C3B/0?term=3][CQ:face,id=212]",
                                    "sender": {
                                        "age": 0,
                                        "area": "",
                                        "card": "gxhTester啊",
                                        "level": "",
                                        "nickname": "LazuliKao",
                                        "role": "owner",
                                        "sex": "unknown",
                                        "title": "",
                                        "user_id": 1
                                    },
                                    "message_id": 632882299
                                }*/

                                //var selfId = json.Value<string>("self_id");
                                var groupId = json.Value<string>("group_id")!;
                                var userId = json.Value<string>("user_id")!;
                                //var message_seq = json.Value<long>("message_seq");
                                var message = json["message"]!.ToObject<List<MessageEntity>>()!;
                                var rawMessage = json.Value<string>("raw_message")!;
                                var sender = json["sender"]!;
                                var card = sender.Value<string>("card");
                                var msgId = json.Value<string>("message_id")!;
                                if (QQ == userId) //自己发送的消息
                                {
                                    break;
                                }
                                Events.Events.CallOnGroupMessageReceived(
                                    new GroupMessageEventArgs(
                                        () => new OneBotCommandReader(service, message),
                                        async () => await api.GetGroupNameAsync(groupId)
                                    )
                                    {
                                        Service = service,
                                        MessageId = msgId,
                                        GroupId = groupId,
                                        SenderId = userId,
                                        SenderMemberCard = string.IsNullOrWhiteSpace(card)
                                            ? sender.Value<string>("nickname") ?? ""
                                            : card,
                                        TextMessageLazy = new(() => rawMessage)
                                    }
                                );
                            }
                            break;
                    }
                }
                break;
            case "notice":
                {
                    var noticeType = json.Value<string>("notice_type");
                    switch (noticeType)
                    {
                        case "group_increase":
                            {
                                //{
                                //  "post_type": "notice",
                                //  "notice_type": "group_increase",
                                //  "time": 1659604306,
                                //  "self_id": 1,
                                //  "sub_type": "approve",
                                //  "operator_id": 0,
                                //  "user_id": 1,
                                //  "group_id": 1
                                //}
                            }
                            break;
                        case "group_decrease":
                            {
                                //{
                                //  "post_type": "notice",
                                //  "notice_type": "group_decrease",
                                //  "time": 1659604362,
                                //  "self_id": 1,
                                //  "sub_type": "kick",
                                //  "group_id": 1,
                                //  "operator_id": 1,
                                //  "user_id": 1
                                //}

                                //{
                                //  "post_type": "notice",
                                //  "notice_type": "group_decrease",
                                //  "time": 1659604427,
                                //  "self_id": 1,
                                //  "sub_type": "leave",
                                //  "group_id": 1,
                                //  "operator_id": 1,
                                //  "user_id": 1
                                //}



                                var selfId = json.Value<string>("self_id");
                                var groupId = json.Value<string>("group_id");
                                var userId = json.Value<string>("user_id");
                                var subType = json.Value<string>("sub_type");
                                var operatorId = json.Value<string>("operator_id");
                                if (subType == "kick")
                                {
                                    //Global
                                    //    .IM
                                    //    .OnLeftGroup
                                    //    .Invoke(
                                    //        new IMEventsMap.LeftGroupEventsArgs(
                                    //            self_id,
                                    //            group_id,
                                    //            user_id,
                                    //            operator_id
                                    //        )
                                    //    );
                                }
                                else
                                {
                                    //Global
                                    //    .IM
                                    //    .OnLeftGroup
                                    //    .Invoke(
                                    //        new IMEventsMap.LeftGroupEventsArgs(
                                    //            self_id,
                                    //            group_id,
                                    //            user_id,
                                    //            ""
                                    //        )
                                    //    );
                                }
                            }
                            break;
                        case "group_upload":
                            {
                                //                                    {
                                //  "post_type": "notice",
                                //  "notice_type": "group_upload",
                                //  "time": 1659604012,
                                //  "self_id": 1,
                                //  "user_id": 1,
                                //  "file": {
                                //    "busid": 102,
                                //    "id": "/d025895e-13d4-11ed-8f00-525400ec8750",
                                //    "name": "ILSpy.exe.lnk",
                                //    "size": 1049,
                                //    "url": "https://"
                                //  },
                                //  "group_id": 1
                                //}
                                var groupId = json.Value<string>("group_id");
                                var userId = json.Value<string>("user_id");
                                var selfId = json.Value<string>("self_id");
                                var file = json.Value<JObject>("file")!;
                                var fileName = file.Value<string>("name");
                                var fileSize = file.Value<string>("size");
                                //Global
                                //    .IM
                                //    .OnGroupFile
                                //    .Invoke(
                                //        new IMEventsMap.GroupFileEventsArgs(
                                //            self_id,
                                //            group_id,
                                //            user_id,
                                //            file_name,
                                //            file_size
                                //        )
                                //    );
                            }
                            break;
                        default:
                            break;
                    }
                }
                break;
            default:
#if DEBUG
                service.LogDebug(json.ToString());
#endif
                break;
        }
#if !DEBUG
        }
        catch (Exception e)
        {
            service.LogError(nameof(ProcessMessageAsync) + " " + data, e);
        }
#endif
    }
}
