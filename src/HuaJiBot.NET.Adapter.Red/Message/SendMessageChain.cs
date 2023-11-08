using Newtonsoft.Json;
using RedProtocolSharp.Message;
using System.Runtime.InteropServices.JavaScript;

namespace HuaJiBot.NET.Adapter.Red.Message;

internal static class SendMessageChain
{
    internal static async Task<SendEcho?> Send(this Connector connector, MessageChain chain)
    {
        var payload = new MessageSend
        {
            elements = new List<Elements>(),
            peer = new Peer
            {
                chatType = chain.chatTypes switch
                {
                    ChatTypes.GroupMessage => 2,
                    ChatTypes.PrivateMessage => 1,
                    _ => 0
                },
                peerUin = chain.PeerUin
            }
        };
        foreach (var item in chain)
            switch (item)
            {
                case SendAtElement data:
                {
                    payload.elements.Add(
                        new Elements
                        {
                            elementType = 1,
                            textElement = new TextElement { atType = 2, atNtUin = data.target }
                        }
                    );
                    payload.elements.Add(
                        new Elements
                        {
                            elementType = 1,
                            textElement = new TextElement { content = " " }
                        }
                    );
                    break;
                }
                case SendReplyElement data:
                {
                    payload.elements.Add(
                        new Elements
                        {
                            elementType = 7,
                            replyElement = new ReplyElement
                            {
                                replyMsgSeq = data.replyMsgSeq,
                                senderUinStr = data.replyTargetUin
                            }
                        }
                    );
                    break;
                }
                case SendTextElement data:
                {
                    payload.elements.Add(
                        new Elements
                        {
                            elementType = 1,
                            textElement = new TextElement { content = data.content }
                        }
                    );
                    break;
                }
                case SendImageElement data:
                {
                    var uploadNode = "upload";
                    var fileName = Path.GetFileName(data.sourcePath);
                    var uploadReply = await connector.HttpPostUpload(data.sourcePath, uploadNode);
                    var picReply = new UploadData();
                    if (uploadReply != "")
                        picReply = JsonConvert.DeserializeObject<UploadData>(uploadReply);
                    else
                        return null;
                    payload.elements.Add(
                        new Elements
                        {
                            elementType = 2,
                            picElement = new PicElement
                            {
                                md5HexStr = picReply.md5,
                                fileSize = picReply.fileSize,
                                fileName = fileName,
                                sourcePath = picReply.ntFilePath,
                                picHeight = picReply.imageInfo.height,
                                picWidth = picReply.imageInfo.width
                            }
                        }
                    );
                    break;
                }
                case SendVoiceElement data:
                {
                    var uploadNode = "upload";
                    var uploadReply = await connector.HttpPostUpload(data.filePath, uploadNode);
                    var voiceReply = new UploadData();
                    if (uploadReply != "")
                        voiceReply = JsonConvert.DeserializeObject<UploadData>(uploadReply);
                    else
                        return null;
                    if (voiceReply == null)
                        return null;
                    payload.elements.Add(
                        new Elements
                        {
                            elementType = 4,
                            pttElement = new PttElement
                            {
                                md5HexStr = voiceReply.md5,
                                duration = data.duration,
                                fileName = Path.GetFileName(voiceReply.ntFilePath),
                                filePath = voiceReply.ntFilePath,
                                fileSize = voiceReply.fileSize,
                                waveAmplitudes = new[] { 0, 1, 3, 3, 1, 0 }
                            }
                        }
                    );
                    break;
                }
            }
        var package = JsonConvert.SerializeObject(payload);
        Console.WriteLine("test " + package);
        var reply = await connector.HttpPostRequest(package, "message/send");
        var echo = JsonConvert.DeserializeObject<SendEcho>(reply);
        if (echo != null)
            echo.chatTypes = echo.chatType switch
            {
                1 => ChatTypes.PrivateMessage,
                2 => ChatTypes.GroupMessage,
                _ => echo.chatTypes
            };
        return echo;
    }

    #region Echo

    public class SendEcho
    {
        public int? chatType { get; set; }
        public string? msgId { get; set; }
        public string? msgSeq { get; set; }
        public string? msgTime { get; set; }
        public string? senderUin { get; set; }
        public string? sendMemberName { get; set; }
        public string? sendNickName { get; set; }
        public string? peerUin { get; set; }
        public string? peerName { get; set; }

        [JsonIgnore]
        public ChatTypes chatTypes { get; set; }
    }

    #endregion
}
