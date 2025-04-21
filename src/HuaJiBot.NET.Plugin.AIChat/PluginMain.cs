using System.ClientModel;
using System.Text;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace HuaJiBot.NET.Plugin.AIChat;

public class PluginConfig : ConfigBase
{
    public string Endpoint = "";
    public string ApiKey = "";
    public string Model = "huihui_ai/qwen2.5-1m-abliterated:14b";
    public string SystemPrompt = "你是一个有用的AI助手";
    public bool OpenAILogging = false;
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    private OpenAIClient? _client = null;
    private string _clientApiKey = "";
    private string _clientModel = "";

    private OpenAIClient Client
    {
        get
        {
            if (
                _client is null //首次获取
                || _clientApiKey != Config.ApiKey
                || _clientModel != Config.Model //模型设置有变动
            )
            {
                _client = new OpenAIClient(
                    new ApiKeyCredential(
                        string.IsNullOrEmpty(Config.ApiKey) ? "null" : Config.ApiKey
                    ),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(Config.Endpoint),
                        ClientLoggingOptions = new()
                        {
                            EnableLogging = Config.OpenAILogging,
                            LoggerFactory = LoggerFactory.Create(logger =>
                            {
                                logger.AddProvider(new PluginLoggerProvider(this));
                            }),
                        },
                    }
                );
                _clientApiKey = Config.ApiKey;
                _clientModel = Config.Model;
            }
            return _client;
        }
    }
    private ChatClient ChatClient => Client.GetChatClient(Config.Model);
    private OpenAIModelClient ModelClient => Client.GetOpenAIModelClient();

    private MessageHistory _history = null!;

    protected override void Initialize()
    {
        _history = new MessageHistory(Service, "ai_messages.db");
        Service.Events.OnGroupMessageReceived += (s, e) => _ = Events_OnGroupMessageReceived(e);
        Info("启动成功");
        Task.Run(async () =>
        {
            var models = await ModelClient.GetModelsAsync();
            var s = new StringBuilder("模型列表：");
            foreach (var model in models.Value) { }
        });
    }

    private async Task InvokeLlmMessage(
        IEnumerable<ChatMessage> messages,
        Events.GroupMessageEventArgs e
    )
    {
        Info(
            "调用AI消息\n\t"
                + JsonConvert
                    .SerializeObject(
                        messages,
                        new JsonSerializerSettings
                        {
                            Formatting = Formatting.Indented,
                            NullValueHandling = NullValueHandling.Ignore,
                        }
                    )
                    .Replace("\n", "\n\t")
        );
        var response = await ChatClient.CompleteChatAsync(messages);
        foreach (var content in response.Value.Content)
        {
            switch (content.Kind)
            {
                case ChatMessageContentPartKind.Text:
                    var text = content.Text;
                    var messageIds = await e.Reply(content.Text);
                    //机器人回复后把自己的消息添加到数据库
                    foreach (var msgId in messageIds)
                    {
                        _history.StoreMessage( //AI回复记录
                            new GroupMessage
                            {
                                Content = text,
                                GroupId = e.GroupId,
                                MessageId = msgId,
                                SenderId = null,
                                SenderName = "bot",
                                IsBot = true,
                                ReplyToMessageId = e.MessageId,
                            }
                        );
                    }
                    break;
            }
        }
    }

    private async Task Events_OnGroupMessageReceived(Events.GroupMessageEventArgs e)
    {
        var reader = e.CommandReader;
        if (reader.At(out var atId))
        {
            if (!Service.AllRobots.Contains(atId))
                return; //仅处理At机器人
            if (reader.Input(out var restText, true))
            {
                try
                {
                    //收到at机器人的消息则处理
                    Info($"收到At消息@{atId}:{restText}");
                    //把消息存到数据中以便多轮会话查阅
                    _history.StoreMessage( // 收到消息记录
                        new GroupMessage
                        {
                            Content = restText,
                            GroupId = e.GroupId,
                            MessageId = e.MessageId,
                            SenderId = e.SenderId,
                            SenderName = await e.GetGroupNameAsync(),
                            IsBot = false,
                            ReplyToMessageId = null,
                        }
                    );
                    //调用LLM回复
                    await InvokeLlmMessage(
                        [
                            ChatMessage.CreateSystemMessage(Config.SystemPrompt),
                            ChatMessage.CreateUserMessage(restText),
                        ],
                        e
                    );
                }
                catch (Exception exception)
                {
                    Error("调用AI失败", exception);
                }
            }
        }
        reader = e.CommandReader;
        if (reader.Reply(out var data))
        {
            _ = reader.Input(out var text);
            text ??= "";
            SortedList<DateTime, GroupMessage> messageList = [];
            void PrependMessage(GroupMessage message)
            {
                messageList.Add(message.Timestamp, message);
                if (messageList.Count > 100)
                { //限制最大数量
                    return;
                }
                //如果有父消息，则继续提取
                if (message.ReplyToMessageId is { } parentReplyMessageId)
                {
                    FetchReply(parentReplyMessageId);
                }
            }
            Info("Reply -> messageId: " + data);

            void FetchReply(string replyId)
            {
                var replyMessage = _history.GetMessage(replyId);
                if (replyMessage is not null)
                { //获取到被回复的消息
                    PrependMessage(replyMessage);
                }
            }

            #region 提取相关的消息记录
            string? replyMessageId = null;
            if (data.messageId is not null)
            { //有messageId 直接提取
                replyMessageId = data.messageId;
                FetchReply(replyMessageId);
            }
            else
            { //没messageId，根据内容模糊匹配
                if (data is { content: { } replyContent })
                {
                    var replyMessage =
                        _history.GetGroupMessageLastEndWith(e.GroupId, replyContent)
                        ?? _history.GetGroupMessageLastSimilar(e.GroupId, replyContent);
                    if (replyMessage is not null)
                    { //获取到被回复的消息
                        replyMessageId = replyMessage.MessageId;
                        PrependMessage(replyMessage);
                    }
                }
            }
            #endregion
            //如果所有回复上下文都与bot无关，则不处理
            if (messageList.All(x => !x.Value.IsBot))
            {
                return;
            }
            #region 调用大模型回复（多轮对话）
            List<ChatMessage> prompts = [ChatMessage.CreateSystemMessage(Config.SystemPrompt)];
            foreach (var (_, message) in messageList)
            {
                prompts.Add(
                    message.IsBot
                        ? ChatMessage.CreateAssistantMessage(message.Content)
                        : ChatMessage.CreateUserMessage(message.Content)
                );
            }
            prompts.Add(ChatMessage.CreateUserMessage(text));
            try
            {
                //收到回复消息记录
                _history.StoreMessage(
                    new GroupMessage
                    {
                        Content = text,
                        GroupId = e.GroupId,
                        MessageId = e.MessageId,
                        SenderId = e.SenderId,
                        SenderName = await e.GetGroupNameAsync(),
                        IsBot = false,
                        ReplyToMessageId = replyMessageId,
                    }
                );
                //调用LLM回复
                await InvokeLlmMessage(prompts, e);
            }
            catch (Exception exception)
            {
                Error("调用AI失败", exception);
            }

            #endregion
        }
    }

    protected override void Unload() { }

    public PluginConfig Config { get; } = new();
}
