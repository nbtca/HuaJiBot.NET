using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HuaJiBot.NET.Adapter.OneBot;

internal class OneBotMessageHandler(BotServiceBase service, Action<string> send)
{
    private string? _qq = null;
    private OneBotApi _api = new(service, send);

    public async Task ProcessMessageAsync(string data)
    {
        try
        {
            var json = JObject.Parse(data);
            if (json.ContainsKey("echo"))
            {
                await _api.ProcessMessageAsync(json);
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
                            //  "self_id":3623498320,
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
                                    var info = await _api.GetVersionInfoAsync();
#if DEBUG
                                    service.Log(JsonConvert.SerializeObject(info));
#endif
                                    var appName = info["app_name"];
                                    var appVersion = info["app_version"];
                                    if (info.TryGetValue("nt_protocol", out var protocolVersion))
                                    {
                                        appName = appName + " v" + appVersion;
                                        appVersion = protocolVersion;
                                    }
                                    Events
                                        .Events
                                        .CallOnBotLogin(
                                            service,
                                            new BotLoginEventArgs
                                            {
                                                AccountId = _qq ?? "-1",
                                                ClientName = appName,
                                                ClientVersion = appVersion,
                                                Service = service
                                            }
                                        );
                                }
                                break;
                            //{"post_type":"meta_event","meta_event_type":"heartbeat","time":1659587845,"self_id":3623498320,"status":{"app_enabled":true,"app_good":true,"app_initialized":true,"good":true,"online":true,"plugins_good":null,"stat":{"packet_received":75,"packet_sent":66,"packet_lost":0,"message_received":0,"message_sent":0,"last_message_time":0,"disconnect_times":0,"lost_times":0}},"interval":5000}
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
                                        "time": 1659601722,
                                        "self_id": 3623498320,
                                        "sub_type": "normal",
                                        "group_id": 626872357,
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
                                                    "qq": "441870948"
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
                                                    "url": "https://gchat.qpic.cn/gchatpic_new/441870948/626872357-2551983170-9183AB6F146208A5F8F3835E02A4BE6C/0?term=3"
                                                }
                                            },
                                            {
                                                "type": "image",
                                                "data": {
                                                    "file": "cee8c6bb7b1c5fc4be0a406aa2746c3b.image",
                                                    "subType": "0",
                                                    "url": "https://gchat.qpic.cn/gchatpic_new/441870948/626872357-2945018168-CEE8C6BB7B1C5FC4BE0A406AA2746C3B/0?term=3"
                                                }
                                            },
                                            {
                                                "type": "face",
                                                "data": {
                                                    "id": "212"
                                                }
                                            }
                                        ],
                                        "user_id": 441870948,
                                        "anonymous": null,
                                        "font": 0,
                                        "message_seq": 26460,
                                        "raw_message": "[CQ:reply,id=2001268508][CQ:at,qq=441870948] [CQ:image,file=9183ab6f146208a5f8f3835e02a4be6c.image,subType=0,url=https://gchat.qpic.cn/gchatpic_new/441870948/626872357-2551983170-9183AB6F146208A5F8F3835E02A4BE6C/0?term=3][CQ:image,file=cee8c6bb7b1c5fc4be0a406aa2746c3b.image,subType=0,url=https://gchat.qpic.cn/gchatpic_new/441870948/626872357-2945018168-CEE8C6BB7B1C5FC4BE0A406AA2746C3B/0?term=3][CQ:face,id=212]",
                                        "sender": {
                                            "age": 0,
                                            "area": "",
                                            "card": "gxhTester啊",
                                            "level": "",
                                            "nickname": "LazuliKao",
                                            "role": "owner",
                                            "sex": "unknown",
                                            "title": "",
                                            "user_id": 441870948
                                        },
                                        "message_id": 632882299
                                    }*/

                                    var selfId = json.Value<string>("self_id");
                                    var groupId = json.Value<string>("group_id");
                                    var userId = json.Value<string>("user_id");
                                    //var message_seq = json.Value<long>("message_seq");
                                    var message = json.Value<JArray>("message");
                                    var rawMessage = json.Value<string>("raw_message");
                                    //var message_id = json.Value<long>("message_id");
                                    //var anonymous = json.Value<JObject>("anonymous");
                                    //Events.Events.CallOnGroupMessageReceived( todo
                                    //Global
                                    //     .IM
                                    //     .OnGroupMessage
                                    //     .Invoke(
                                    //         new IMEventsMap.GroupMessageEventsArgs(
                                    //             self_id,
                                    //             group_id,
                                    //             user_id,
                                    //             raw_message,
                                    //             message.ToString(Newtonsoft.Json.Formatting.None)
                                    //         )
                                    //     );
                                }
                                break;

                            //                  }
                            //  else if (MQ_消息类型 == 消息类型_某人退出群)
                            //                  {
                            //                      PFBridgeCore.Global.IM.OnLeftGroup.Invoke(new IMEventsMap.LeftGroupEventsArgs(
                            //                           MQ_机器人QQ, MQ_消息来源, MQ_触发对象_主动, ""));
                            //                  }
                            //                  else if (MQ_消息类型 == 消息类型_群文件接收)  //群消息
                            //                  {
                            //                      ////{
                            //                      ////    "fileid": "/35415d8c-09d4-11ed-af9a-5254003b73a0",
                            //                      ////    "filename": "EModuleViewEcinfo.exe.lnk",
                            //                      ////    "filesize": "1.44kb",
                            //                      ////    "filelink": "http://njc-download.ftn.qq.com/ftn_handler/2DE895415BFD8EBDE5ECF431DB746370894DB142297460EFB437EB142B7C31E71664F98EDD9882723DFCB1F0F8EFC86E021A10BCDAC7E7859724B9A37852FAFE"
                            //                      ////}
                            //                      var json = JObject.Parse(MQ_消息内容);
                            //                      PFBridgeCore.Global.IM.OnGroupFile.Invoke(new IMEventsMap.GroupFileEventsArgs(
                            //                        MQ_机器人QQ, MQ_消息来源, MQ_触发对象_主动, json.Value<string>("filename"), json.Value<string>("filesize")));
                            //                  }
                            //                  else if (MQ_消息类型 == 消息类型_群)  //群消息
                            //                  {
                            //                      PFBridgeCore.Global.IM.OnGroupMessage.Invoke(new IMEventsMap.GroupMessageEventsArgs(
                            //                          MQ_机器人QQ, MQ_消息来源, MQ_触发对象_主动, MQ_消息内容));
                            //                  }
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
                                    //  "self_id": 3623498320,
                                    //  "sub_type": "approve",
                                    //  "operator_id": 0,
                                    //  "user_id": 1215023305,
                                    //  "group_id": 626872357
                                    //}
                                }
                                break;
                            case "group_decrease":

                                {
                                    //{
                                    //  "post_type": "notice",
                                    //  "notice_type": "group_decrease",
                                    //  "time": 1659604362,
                                    //  "self_id": 3623498320,
                                    //  "sub_type": "kick",
                                    //  "group_id": 626872357,
                                    //  "operator_id": 441870948,
                                    //  "user_id": 1215023305
                                    //}

                                    //{
                                    //  "post_type": "notice",
                                    //  "notice_type": "group_decrease",
                                    //  "time": 1659604427,
                                    //  "self_id": 3623498320,
                                    //  "sub_type": "leave",
                                    //  "group_id": 626872357,
                                    //  "operator_id": 1215023305,
                                    //  "user_id": 1215023305
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
                                    //  "self_id": 3623498320,
                                    //  "user_id": 441870948,
                                    //  "file": {
                                    //    "busid": 102,
                                    //    "id": "/d025895e-13d4-11ed-8f00-525400ec8750",
                                    //    "name": "ILSpy.exe.lnk",
                                    //    "size": 1049,
                                    //    "url": "http://36.155.248.61/ftn_handler/e24b579ee99867aaf572f2b603ba68d218814f72cbadc5471e206d61827b221635a658d22cdac4983e95dceff0af5c4114888146ce40f417f64af70677dcb8b4/?fname=2f64303235383935652d313364342d313165642d386630302d353235343030656338373530"
                                    //  },
                                    //  "group_id": 626872357
                                    //}
                                    var groupId = json.Value<string>("group_id");
                                    var userId = json.Value<string>("user_id");
                                    var selfId = json.Value<string>("self_id");
                                    var file = json.Value<JObject>("file");
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
        }
        catch (Exception e)
        {
#if DEBUG
            throw;
#endif
            service.LogError(nameof(ProcessMessageAsync) + " " + data, e.Message);
        }
    }
}
