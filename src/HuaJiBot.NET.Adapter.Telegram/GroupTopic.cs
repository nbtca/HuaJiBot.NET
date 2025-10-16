namespace HuaJiBot.NET.Adapter.Telegram;

internal class GroupTopic(long groupId, int? topicId)
{
    public static GroupTopic Parse(string combinedId)
    {
        var ids = combinedId.Split(':');
        return new GroupTopic(long.Parse(ids[0]), ids.Length == 1 ? null : int.Parse(ids[1]));
    }

    public long GroupId => groupId;
    public int? TopicId => topicId;
    public bool HasTopic => TopicId is { };

    public static implicit operator string(GroupTopic item) => item.ToString();

    public override string ToString()
    {
        if (TopicId is { } topic)
            return GroupId + ":" + topic;
        return GroupId.ToString();
    }
}
