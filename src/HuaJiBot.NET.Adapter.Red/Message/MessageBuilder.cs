#region

using RedProtocolSharp.Message;

#endregion

namespace HuaJiBot.NET.Adapter.Red.Message;

public sealed class MessageBuilder
{
    private readonly MessageChain _chain = new();

    public MessageBuilder SetTarget(string target, ChatTypes chatTypes)
    {
        _chain.PeerUin = target;
        _chain.chatTypes = chatTypes;
        return this;
    }

    public MessageBuilder AddText(string content)
    {
        _chain.Add(new SendTextElement { content = content });
        return this;
    }

    public MessageBuilder AddAt(string target)
    {
        _chain.Add(new SendAtElement { target = target });
        return this;
    }

    public MessageBuilder AddReply(string replayMsgSeq, string replyMsgId, string targetUin)
    {
        _chain.Add(new SendReplyElement { replyMsgSeq = replayMsgSeq, replyTargetUin = targetUin });
        return this;
    }

    public MessageBuilder AddPic(string filePath)
    {
        _chain.Add(new SendImageElement { sourcePath = filePath });
        return this;
    }

    //public MessageBuilder AddVoice(string filePath)
    //{
    //    try
    //    {
    //        var voiceInfo = VoiceConverter.Mp3ToSilk(filePath);
    //        if (voiceInfo != null)
    //            _chain.Add(new SendVoiceElement
    //            {
    //                filePath = voiceInfo.filePath,
    //                name = voiceInfo.name,
    //                duration = voiceInfo.duration
    //            });
    //    }
    //    catch (Exception e)
    //    {
    //        return this;
    //    }
    //    return this;
    //}

    public MessageChain Build() => _chain;
}
