using System.ClientModel;
using System.ClientModel.Primitives;
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
                    new OpenAIClientOptions { Endpoint = new Uri(Config.Endpoint) }
                );
                _clientApiKey = Config.ApiKey;
                _clientModel = Config.Model;
            }
            return _client;
        }
    }
    private ChatClient ChatClient => Client.GetChatClient(Config.Model);
    private OpenAIModelClient ModelClient => Client.GetOpenAIModelClient();

    protected override void Initialize()
    {
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
                    Info($"收到At消息@{atId}:{restText}");
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
                                e.Reply(content.Text);
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
        if (reader.Reply(out var messageId))
        {
            Info(messageId);
        }
    }

    protected override void Unload() { }

    public PluginConfig Config { get; } = new();
}
