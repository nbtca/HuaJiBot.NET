namespace RedProtocolSharp.Message;

public interface ISendMessageElement
{
    public string ToSummary();
}

public class SendTextElement : ISendMessageElement
{
    public string content { get; set; } = "";

    public string ToSummary()
    {
        return $@"[Message:Text,Content=""{content}""]";
    }
}

public class SendAtElement : ISendMessageElement
{
    public string target { get; set; } = "";
    public string content { get; set; } = "";

    public string ToSummary()
    {
        return $@"[Message:At,Target=""{target}"",Content=""{content}""]";
    }
}

public class SendImageElement : ISendMessageElement
{
    public string md5 { get; set; } = "";
    public string name { get; set; } = "";
    public string url { get; set; } = "";
    public string sourcePath { get; set; } = "";
    public string size { get; set; } = "";

    public string ToSummary()
    {
        return $@"[Message:Image,Url=""{url}""]";
    }
}

public class SendVoiceElement : ISendMessageElement
{
    public string md5 { get; set; } = "";
    public string name { get; set; } = "";
    public string filePath { get; set; } = "";
    public string size { get; set; } = "";
    public int duration { get; set; }
    public int[]? waveAmplitudes { get; set; }

    public string ToSummary()
    {
        return $@"[Message:Voice,ConfigKey=""{name}""]";
    }
}

public class SendReplyElement : ISendMessageElement
{
    public string replyTargetUin { get; set; }
    public string replyMsgSeq { get; set; }

    public string ToSummary()
    {
        return $@"[Message:Reply,Uin=""{replyTargetUin}"",Seq=""{replyMsgSeq}""]";
    }
}

public class SendMultiForwardElement : ISendMessageElement
{
    public string resId { get; set; }
    public string xmlContent { get; set; }

    public string ToSummary()
    {
        return $@"[Message:Forward,ResId=""{resId}""]";
    }
}
