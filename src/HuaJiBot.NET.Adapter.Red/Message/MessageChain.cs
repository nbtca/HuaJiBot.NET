namespace HuaJiBot.NET.Adapter.Red.Message;

public class MessageChain : List<ISendMessageElement>
{
    internal MessageChain() { }

    public enum Role
    {
        Member = 2,
        Admin = 3,
        Owner = 4
    }

    public ChatTypes chatTypes { get; set; }
    public string SenderUin { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string PeerUin { get; set; } = "";
    public string GroupName { get; set; } = "";
    public Role? RoleType { get; set; }
    public DateTime Time { get; set; }
    public string MsgSeq { get; set; } = "";
    public string MsgId { get; set; } = "";
}
