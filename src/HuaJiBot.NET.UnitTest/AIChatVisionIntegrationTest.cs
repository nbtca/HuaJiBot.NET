using System.Reflection;
using HuaJiBot.NET.Adapter.OneBot;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Config;
using HuaJiBot.NET.Logger;
using HuaJiBot.NET.Plugin.AIChat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.UnitTest;

/// <summary>Real provider test with fixture OneBot events; never posts messages to QQ.</summary>
internal class AIChatVisionIntegrationTest
{
    [Test]
    [Explicit("Requires HUAJIBOT_VISION_TEST_CONFIG and calls its real AI provider")]
    public async Task RealModel_OneBotFixture_ImageFollowupTextAndDisabledVision()
    {
        var path = Environment.GetEnvironmentVariable("HUAJIBOT_VISION_TEST_CONFIG");
        Assert.That(path, Is.Not.Null.And.Not.Empty);
        var config = JsonConvert.DeserializeObject<HuaJiBot.NET.Config.Config>(File.ReadAllText(path!))!;
        var directory = Path.Combine(Path.GetTempPath(), "huajibot-vision-test-" + Guid.NewGuid().ToString("N"));
        var adapter = new FixtureAdapter(directory) { Logger = new ConsoleLogger() };
        await new Internal().SetupServiceAsync(adapter, config);
        var plugin = new PluginMain();
        typeof(PluginBase).GetProperty(nameof(PluginBase.Service))!.SetValue(plugin, adapter);
        typeof(PluginBase).GetProperty(nameof(PluginBase.Name))!.SetValue(plugin, "AIChat");
        JsonConvert.PopulateObject(config.Plugins["AIChat"].ToString(), plugin.Config);
        plugin.Config.GroupIds = ["vision-fixture"];
        plugin.Config.McpServers = [];
        plugin.Config.SupportsVision = true;
        plugin.Config.SystemPrompt = "请根据图片回答，使用中文，简明扼要。";
        await (Task)typeof(PluginMain).GetMethod("InitializeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(plugin, null)!;
        var handler = new OneBotMessageHandler(new OneBotApi(adapter, _ => { }), adapter);
        const string image = "https://raw.githubusercontent.com/github/explore/main/topics/csharp/csharp.png";
        try
        {
            async Task<string> SendAsync(int id, params JObject[] segments)
            {
                adapter.Next = new(TaskCreationOptions.RunContinuationsAsynchronously);
                await handler.ProcessMessageAsync(new JObject
                {
                    ["post_type"] = "message",
                    ["message_type"] = "group",
                    ["group_id"] = "vision-fixture",
                    ["user_id"] = "fixture-user",
                    ["message_id"] = id,
                    ["sender"] = new JObject { ["nickname"] = "vision fixture", ["role"] = "member" },
                    ["message"] = new JArray(segments),
                });
                return await adapter.Next.Task.WaitAsync(TimeSpan.FromSeconds(100));
            }
            static JObject Segment(string type, JObject data) => new() { ["type"] = type, ["data"] = data };
            var mention = Segment("at", new JObject { ["qq"] = "fixture-bot" });
            var attachment = Segment("image", new JObject { ["url"] = image, ["file"] = "fixture.image" });

            var first = await SendAsync(1, mention, attachment);
            Console.WriteLine("IMAGE_ONLY: " + first);
            Assert.That(first, Does.Contain("紫").Or.Contain("C#").Or.Contain("C Sharp"));
            await Task.Delay(100); // Allow the event handler to persist the captured reply.

            var followup = await SendAsync(2, Segment("reply", new JObject { ["id"] = "fixture-reply-1" }),
                Segment("text", new JObject { ["text"] = "之前图片的主色是什么？只回复颜色。" }));
            Console.WriteLine("IMAGE_FOLLOWUP: " + followup);
            Assert.That(followup, Does.Contain("紫"));
            await Task.Delay(100);

            var text = await SendAsync(3, mention,
                Segment("text", new JObject { ["text"] = "只回复 VISION_TEXT_OK" }));
            Console.WriteLine("TEXT_ONLY: " + text);
            Assert.That(text, Does.Contain("VISION_TEXT_OK"));
            await Task.Delay(100);

            plugin.Config.SupportsVision = false;
            var disabled = await SendAsync(4, mention, attachment);
            Console.WriteLine("VISION_DISABLED: " + disabled);
            Assert.That(disabled, Does.Contain("未启用"));
        }
        finally
        {
            typeof(PluginMain).GetMethod("Unload", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plugin, null);
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FixtureAdapter(string directory) : OneBotAdapter("ws://unused", null)
    {
        internal TaskCompletionSource<string> Next = new();
        private int _replies;
        public override string[] AllRobots => ["fixture-bot"];
        protected override Task SetupServiceAsyncCore() => Task.CompletedTask;
        protected override string GetPluginDataPathCore() => directory;
        public override Task<string[]> SendGroupMessageAsync(string? robotId, string targetGroup, params SendingMessageBase[] messages)
        {
            Next.TrySetResult(string.Concat(messages.OfType<TextMessage>().Select(message => message.Text)));
            return Task.FromResult(new[] { "fixture-reply-" + ++_replies });
        }
        public override Task<string[]> SendRichMessageAsync(string? robotId, string targetGroup, RichContent content,
            Func<Task<SendingMessageBase[]>> fallback)
        {
            Next.TrySetResult(content.Markdown);
            return Task.FromResult(new[] { "fixture-reply-" + ++_replies });
        }
    }
}
