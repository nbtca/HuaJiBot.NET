using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Plugin.AIChat.Config;
using HuaJiBot.NET.Plugin.AIChat.Service.Connector;
using HuaJiBot.NET.Plugin.AIChat.Service;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace HuaJiBot.NET.Plugin.AIChat;

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    private MessageHistory _history = null!;
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    private AgentConnector Connector
    {
        get
        {
            return Config.Model switch
            {
                { Provider: ModelProvider.OpenAI } => new OpenAIAgentConnector(
                    Service,
                    Config.Model
                ),
                { Provider: ModelProvider.Google } => new GoogleAgentConnector(
                    Service,
                    Config.Model
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(Config.Model.Provider)),
            };
        }
    }

    private AIFunction[]? _tools;

    private AIFunction[] GetTools() =>
        _tools ??= AgentTools.CreateBotFunctions(Service.ExportFunctions);

    protected override void Initialize()
    {
        _history = new MessageHistory(Service, "ai_messages.db");
        Service.Events.OnGroupMessageReceived += (s, e) => _ = Events_OnGroupMessageReceived(e);
        Info("启动成功");
        //Task.Run(async () =>
        //{
        //    //var models = await ModelClient.GetModelsAsync();
        //    //Info("模型列表：" + string.Join(", ", models.Value.Select(x => x.ModelId)));
        //});
    }

    private AgentSession GetOrCreateSession(string groupId)
    {
        return _sessions.GetOrAdd(groupId, _ =>
        {
            var session = Connector.CreateSessionAsync().GetAwaiter().GetResult();
            Info($"为群组 {groupId} 创建新的Agent会话");
            return session;
        });
    }

    private void LogToolCalls(AgentResponseUpdate update)
    {
        foreach (var content in update.Contents)
        {
            if (content is FunctionCallContent funcCall)
            {
                Info($"工具调用: {funcCall.Name}({JsonConvert.SerializeObject(funcCall.Arguments)})");
            }
            else if (content is FunctionResultContent funcResult)
            {
                Info($"工具结果: {funcResult.CallId} = {funcResult.Result}");
            }
        }
    }

    private async Task InvokeLlmMessage(
        string systemPrompt,
        IList<ChatMessage> messages,
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
        var session = GetOrCreateSession(e.GroupId);
        var agent = Connector.CreateAIAgentWithOptions(systemPrompt, GetTools());
        var response = await agent.RunAsync(messages, session);
        // 记录工具调用
        foreach (var update in response.ToAgentResponseUpdates())
        {
            LogToolCalls(update);
        }
        var text = response.Text ?? "null";
        var messageIds = await e.Reply(text);
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
    }

    private async Task InvokeLlmMessageStreaming(
        string systemPrompt,
        IList<ChatMessage> messages,
        Events.GroupMessageEventArgs e
    )
    {
        Info(
            "调用AI流式消息\n\t"
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
        var session = GetOrCreateSession(e.GroupId);
        var agent = Connector.CreateAIAgentWithOptions(systemPrompt, GetTools());
        var sb = new StringBuilder();
        await foreach (var update in agent.RunStreamingAsync(messages, session))
        {
            // 记录工具调用
            LogToolCalls(update);
            if (!string.IsNullOrEmpty(update.Text))
            {
                sb.Append(update.Text);
                Info($"流式文本块: {update.Text}");
            }
        }
        var text = sb.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "null";
        }
        var messageIds = await e.Reply(text);
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
                        Config.SystemPrompt,
                        [new ChatMessage(ChatRole.User, restText)],
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
            try
            {
                _ = reader.Input(out var text, true);
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
                List<ChatMessage> prompts = [];
                foreach (var (_, message) in messageList)
                {
                    prompts.Add(
                        message.IsBot
                            ? new ChatMessage(ChatRole.Assistant, message.Content)
                            : new ChatMessage(ChatRole.User, message.Content)
                    );
                }
                prompts.Add(new ChatMessage(ChatRole.User, text));

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
                await InvokeLlmMessage(Config.SystemPrompt, prompts, e);
                #endregion
            }
            catch (Exception exception)
            {
                Error("多轮对话调用失败", exception);
            }
        }
    }

    protected override void Unload() { }

    public PluginConfig Config { get; } = new();
}
