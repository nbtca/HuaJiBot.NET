﻿using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.Red;

#region BaseType
public class Payload //载荷
{
    [JsonProperty("type")]
    public required string Type { get; set; }
}

public class Payload<T> : Payload
{
    [JsonProperty("payload")]
    public required T Data { get; set; }

    internal MessageInfo GetMessageInfo()
    {
        var messageInfo = new MessageInfo();
        if (Data is MessageRecv data)
        {
            messageInfo.ChatType = data.chatType;
            messageInfo.Uin = data.SenderId;
            messageInfo.SenderName = string.IsNullOrEmpty(data.sendMemberName)
                ? data.sendNickName
                : data.sendMemberName;
            messageInfo.PeerUin = data.PeerUin;
            messageInfo.PeerName = data.peerName;
            messageInfo.Time = DateTimeOffset
                .FromUnixTimeSeconds(long.Parse(data.msgTime))
                .DateTime;
            messageInfo.MsgId = data.msgId;
            messageInfo.ReplayMsgSeq = data.msgSeq;
            messageInfo.RoleType = data.roleType;
        }

        return messageInfo;
    }
    //获取消息相关信息
}
#endregion

#region Helper
public class MessageInfo
{
    public string? Uin { get; set; }
    public string? SenderName { get; set; }
    public string? PeerUin { get; set; } //私聊时为发送者QQ号
    public string? PeerName { get; set; }
    public int? ChatType { get; set; } //1为私聊,2为群聊
    public DateTime Time { get; set; }
    public string? ReplayMsgSeq { get; set; }
    public string? MsgId { get; set; }
    public int? RoleType { get; set; }
}
#endregion

#region connect
public class ConnectSend //连接请求格式
{
    public string? token { get; set; }
}

public class ConnectRecv //连接返回格式
{
    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("authData")]
    public AuthData? AuthData { get; set; }
}

public class AuthData
{
    [JsonProperty("account")]
    public string? Account { get; set; }

    [JsonProperty("mainAccount")]
    public string? MainAccount { get; set; }

    [JsonProperty("uin")]
    public string? Uin { get; set; }

    [JsonProperty("uid")]
    public string? Uid { get; set; }

    [JsonProperty("nickName")]
    public string? NickName { get; set; }

    [JsonProperty("gender")]
    public long Gender { get; set; }

    [JsonProperty("age")]
    public long Age { get; set; }

    [JsonProperty("faceUrl")]
    public string? FaceUrl { get; set; }

    [JsonProperty("a2")]
    public string? A2 { get; set; }

    [JsonProperty("d2")]
    public string? D2 { get; set; }

    [JsonProperty("d2key")]
    public string? D2Key { get; set; }
}

#endregion

#region messageRecv
public class MessageRecv
{
    public string? senderUid { get; set; }
    public string? msgId { get; set; } //消息ID
    public string? msgRandom { get; set; }
    public string? msgSeq { get; set; }
    public string? cntSeq { get; set; }
    public int? chatType { get; set; }
    public int? msgType { get; set; }
    public int? subMsgType { get; set; }
    public int? sendType { get; set; }
    public string? peerUid { get; set; } //群消息群号
    public string? channelId { get; set; }
    public string? guildId { get; set; }
    public string? guildCode { get; set; }
    public string? fromUid { get; set; }
    public string? fromAppid { get; set; }
    public string? msgTime { get; set; } //发送时间
    public string? msgMeta { get; set; }
    public int? sendStatus { get; set; }
    public string? sendMemberName { get; set; }
    public string? sendNickName { get; set; }
    public string? guildName { get; set; }
    public string? channelName { get; set; }

    [JsonProperty("elements")]
    public List<Elements> Elements { get; set; } //消息主体

    //public object? records { get; set; }
    //public object? emojiLikesList { get; set; }
    public string? commentCnt { get; set; }
    public int? directMsgFlag { get; set; }

    //public object? directMsgMembers { get; set; }
    public string? peerName { get; set; }
    public bool? editable { get; set; }
    public string? avatarMeta { get; set; }
    public string? avatarPendant { get; set; }
    public string? feedId { get; set; }
    public string? roleId { get; set; }
    public string? timeStamp { get; set; }
    public bool? isImportMsg { get; set; }
    public int? atType { get; set; }
    public int? roleType { get; set; } //2为成员3为管理4为群主
    public RoleInfo? fromChannelRoleInfo { get; set; }
    public RoleInfo? fromGuildRoleInfo { get; set; }
    public RoleInfo? levelRoleInfo { get; set; }
    public string? recallTime { get; set; }
    public bool? isOnlineMsg { get; set; }
    public string? generalFlags { get; set; }
    public string? clientSeq { get; set; }
    public int? nameType { get; set; }
    public int? avatarFlag { get; set; }
    public string? anonymousExtInfo { get; set; }
    public string? personalMedal { get; set; }
    public string? roleManagementTag { get; set; }

    [JsonProperty("senderUin")]
    public string? SenderId { get; set; }

    [JsonProperty("peerUin")]
    public string? PeerUin { get; set; }
}

public class RoleInfo
{
    public string? roleId { get; set; }
    public string? name { get; set; }
    public string? color { get; set; }
}

public class Elements
{
    public int? elementType { get; set; } //8为gray tip
    public string? elementId { get; set; }
    public string? extBufForUI { get; set; }
    public PicElement? picElement { get; set; }

    [JsonProperty("textElement")]
    public TextElement? TextElement { get; set; }
    public object? arkElement { get; set; }
    public object? avRecordElement { get; set; }
    public object? calendarElement { get; set; }
    public object? faceElement { get; set; }
    public object? fileElement { get; set; }
    public object? giphyElement { get; set; }
    public GrayTipElement? grayTipElement { get; set; }
    public object? inlineKeyboardElement { get; set; }
    public object? liveGiftElement { get; set; }
    public object? markdownElement { get; set; }
    public object? marketFaceElement { get; set; }
    public MultiForwardMsgElement? multiForwardMsgElement { get; set; }
    public PttElement? pttElement { get; set; }
    public ReplyElement? replyElement { get; set; }
    public object? structLongMsgElement { get; set; }
    public object? textGiftElement { get; set; }
    public object? videoElement { get; set; }
    public object? walletElement { get; set; }
    public object? yoloGameResultElement { get; set; }
}

public class GrayTipElement
{
    public XmlElement? xmlElement { get; set; }
    public object? aioOpGrayTipElement { get; set; }
    public object? blockGrayTipElement { get; set; }
    public object? buddyElement { get; set; }
    public object? buddyNotifyElement { get; set; }
    public object? emojiReplyElement { get; set; }
    public object? essenceElement { get; set; }
    public object? feedMsgElement { get; set; }
    public object? fileReceiptElement { get; set; }
    public GroupElement? groupElement { get; set; }
    public object? groupNotifyElement { get; set; }
    public object? jsonGrayTipElement { get; set; }
    public object? localGrayTipElement { get; set; }
    public object? proclamationElement { get; set; }
    public object? revokeElement { get; set; }
    public object? subElementType { get; set; }
}

public class GroupElement
{
    public int? type { get; set; } //1为加群
    public int? role { get; set; }
    public string? groupName { get; set; } //空
    public string? memberUid { get; set; }
    public string? memberNick { get; set; }
    public string? memberRemark { get; set; }
    public string? adminUid { get; set; } //群主
    public string? adminNick { get; set; }
    public string? adminRemark { get; set; }
    public MemberAdd? memberAdd { get; set; }
    public string? memberUin { get; set; }
    public string? adminUin { get; set; }
}

public class MemberAdd
{
    public string? showType { get; set; }
    public OtherAdd? otherAdd { get; set; }
}

public class OtherAdd
{
    public string? uid { get; set; }
    public string? name { get; set; }
    public string? uin { get; set; }
}

public class PicElement
{
    public int? picSubType { get; set; }
    public string? fileName { get; set; }
    public string? fileSize { get; set; }
    public int? picWidth { get; set; }
    public int? picHeight { get; set; }
    public bool? original { get; set; }
    public string? md5HexStr { get; set; }
    public string? sourcePath { get; set; }
    public object? thumbPath { get; set; }
    public int? transferStatus { get; set; }
    public int? progress { get; set; }
    public int? picType { get; set; }
    public int? invalidState { get; set; }
    public string? fileUuid { get; set; }
    public string? fileSubId { get; set; }
    public int? thumbFileSize { get; set; }
    public string? summary { get; set; }
    public EmojiAd? emojiAd { get; set; }
    public EmojiMail? emojiMall { get; set; }
    public EmojiZplan? emojiZplan { get; set; }
}

public class PttElement
{
    public string? fileName { get; set; }
    public string? filePath { get; set; }
    public string? md5HexStr { get; set; }
    public string? fileSize { get; set; }
    public int duration { get; set; }
    public int[] waveAmplitudes { get; set; }
}

public class MultiForwardMsgElement
{
    public string resId { get; set; }
    public string xmlContent { get; set; }
}

public class EmojiAd
{
    public string? url { get; set; }
    public string? desc { get; set; }
}

public class EmojiMail
{
    public int? packageId { get; set; }
    public int? emojiId { get; set; }
}

public class EmojiZplan
{
    public int? actionId { get; set; }
    public string? actionName { get; set; }
    public int? actionType { get; set; }
    public int? playerNumber { get; set; }
    public string? peerUid { get; set; }
    public string? bytesReserveInfo { get; set; }
}

public class TextElement
{
    [JsonProperty("content")]
    public string? Content { get; set; }

    /// <summary>
    /// 0为无at 1为at全体 2为at某人
    /// content = "@xxx"
    /// </summary>

    [JsonProperty("atType")]
    public int AtType { get; set; }

    [JsonProperty("atUid")]
    public string? AtUid { get; set; }

    [JsonProperty("atTinyId")]
    public string? AtTinyId { get; set; }

    /// <summary>
    ///  被at对象qq号
    /// </summary>

    [JsonProperty("atNtUid")]
    public string? AtNtUid { get; set; }

    [JsonProperty("subElementType")]
    public int SubElementType { get; set; }

    [JsonProperty("atChannelId")]
    public string? AtChannelId { get; set; }

    [JsonProperty("atRoleId")]
    public string? AtRoleId { get; set; }

    [JsonProperty("atRoleColor")]
    public int AtRoleColor { get; set; }

    [JsonProperty("atRoleName")]
    public string? AtRoleName { get; set; }

    [JsonProperty("needNotify")]
    public int NeedNotify { get; set; }
}

public class XmlElement
{
    public string? busiType { get; set; }
    public string? busiId { get; set; }
    public int? c2cType { get; set; }
    public int? serviceType { get; set; }
    public int? ctrlFlag { get; set; }
    public string? content { get; set; }
    public string? templId { get; set; }
    public string? seqId { get; set; }
    public object? templParam { get; set; }
    public string? pbReserv { get; set; }
    public object? members { get; set; }
}

public class ReplyElement
{
    public string? replyMsgSeq { get; set; }
    public string? replyMsgId { get; set; }
    public string? senderUin { get; set; }
    public string? senderUinStr { get; set; }
    public string? senderUid { get; set; }
}
#endregion

#region messageSend

internal class MessageSend
{
    public Peer peer { get; set; }
    public List<Elements> elements { get; set; }
}

internal class Peer
{
    public int chatType { get; set; } //1为私聊2为群聊
    public string peerUin { get; set; }
}

internal class UnsafeMessageSendForwardPayload
{
    public MsgInfos? msgInfos { get; set; }
    public Elements? msgElements { get; set; }
    public string? cover { get; set; }
    public string srcContact { get; set; }
    public string dstContact { get; set; }
}

internal class MsgInfos
{
    public string msgId { get; set; }
    public string snederShowName { get; set; }
}
#endregion

#region info
public class InfoUser
{
    public string? qid { get; set; }
    public string? uin { get; set; } // QQ 号
    public string? nick { get; set; }
    public string? remark { get; set; }
    public string? longNick { get; set; }
    public string? avatarUrl { get; set; }
    public int? birthday_year { get; set; }
    public int? birthday_month { get; set; }
    public int? birthday_day { get; set; }
    public int? sex { get; set; } // 性别
    public string? topTime { get; set; }
    public bool? isBlock { get; set; } // 是否拉黑
    public bool? isMsgDisturb { get; set; }
    public bool? isSpecialCareOpen { get; set; }
    public bool? isSpecialCareZone { get; set; }
    public string? ringId { get; set; }
    public int? status { get; set; }
    public int? extStatus { get; set; }
    public int? categoryId { get; set; }
    public bool? onlyChat { get; set; }
    public bool? qzoneNotWatch { get; set; }
    public bool? qzoneNotWatched { get; set; }
    public bool? vipFlag { get; set; }
    public bool? yearVipFlag { get; set; }
    public bool? svipFlag { get; set; }
    public int? vipLevel { get; set; }
    public string? category { get; set; }
}

public class InfoGroup
{
    public string? groupCode { get; set; } // 群号
    public int? maxMember { get; set; } // 最大人数
    public int? memberCount { get; set; } // 成员人数
    public string? groupName { get; set; } // 群名
    public int? groupStatus { get; set; }
    public int? memberRole { get; set; } // 群成员角色
    public bool? isTop { get; set; }
    public string? toppedTimestamp { get; set; }
    public int? privilegeFlag { get; set; } // 群权限
    public bool? isConf { get; set; }
    public bool? hasModifyConfGroupFace { get; set; }
    public bool? hasModifyConfGroupName { get; set; }
    public string? remarkName { get; set; }
    public bool? hasMemo { get; set; }
    public string? groupShutupExpireTime { get; set; }
    public string? personShutupExpireTime { get; set; }
    public string? discussToGroupUin { get; set; }
    public int? discussToGroupMaxMsgSeq { get; set; }
    public int? discussToGroupTime { get; set; }
}

public class InfoGroupUser
{
    public string? uid { get; set; }
    public int? index { get; set; }
    public UserDetail detail { get; set; }
}

public class UserDetail
{
    public string? uid { get; set; }
    public string? qid { get; set; }
    public string? uin { get; set; } //q号
    public string? nick { get; set; } //qq昵称
    public string? remark { get; set; }
    public int? cardType { get; set; } //群名片
    public int? role { get; set; } //4群主3管理2群员
    public string? avatarPath { get; set; } //头像地址
    public int? shutUpTime { get; set; } //禁言市场
    public bool? isDelete { get; set; }
}
#endregion

#region upload

internal class UploadData
{
    public string? md5 { get; set; }
    public string? fileSize { get; set; }
    public string? filePath { get; set; }
    public string? ntFilePath { get; set; }
    public ImageInfo imageInfo { get; set; }
}

internal class ImageInfo
{
    public int? width { get; set; }
    public int? height { get; set; }
    public string? type { get; set; }
    public string? mime { get; set; }
    public string? wUnits { get; set; }
    public string? hUnits { get; set; }
}

#endregion
