using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using HuaJiBot.NET.DataBase;
using HuaJiBot.NET.Logger;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
                            EnableLogging = true,
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
                    _history.StoreMessage(
                        new GroupMessage
                        {
                            Content = restText,
                            GroupId = e.GroupId,
                            MessageId = e.MessageId,
                            SenderId = e.SenderId,
                            SenderName = await e.GetGroupNameAsync(),
                        }
                    );
                    //调用LLM回复
                    var response = await ChatClient.CompleteChatAsync(
                        [
                            ChatMessage.CreateSystemMessage("你是一个有用的AI助手"),
                            ChatMessage.CreateUserMessage(restText),
                        ]
                    );
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
                                    _history.StoreMessage(
                                        new GroupMessage
                                        {
                                            Content = text,
                                            GroupId = e.GroupId,
                                            MessageId = msgId,
                                            SenderId = null,
                                            SenderName = "bot",
                                        }
                                    );
                                }
                                break;
                        }
                    }
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
            Info("Reply -> messageId: " + data);
            //Service.SendGroupMessageAsync(robot)
        }
    }

    protected override void Unload() { }

    public PluginConfig Config { get; } = new();
}
