using HuaJiBot.NET.Adapter.OneBot.Message;
using HuaJiBot.NET.Adapter.OneBot.Message.Entity;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Commands;

namespace HuaJiBot.NET.Adapter.OneBot;

/// <summary>
/// 指令读取
/// </summary>
internal class OneBotCommandReader(BotService service, List<MessageEntity> msg)
    : CommonCommandReader
{
    public override IEnumerable<ReaderEntity> Msg
    {
        get
        {
            return Parse();

            IEnumerable<ReaderEntity> Parse()
            {
                foreach (var element in msg)
                {
                    switch (element)
                    {
                        case TextMessageEntity { Text: not null and var text }: //文本消息
                            yield return text;
                            break;
                        case AtMessageEntity { At: not null and var userId }: //At消息
                            yield return new ReaderAt(userId);
                            break;
                        default:
                            service.LogDebug($"未转换消息类型：{element.GetType().Name}");
                            break;
                    }
                }
            }
        }
    }
}
