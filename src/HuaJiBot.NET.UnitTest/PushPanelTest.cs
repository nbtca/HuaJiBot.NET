using System.Net;
using System.Text;
using HuaJiBot.NET.Config;
using HuaJiBot.NET.Plugin.PushPanel;
using BridgeConfig = HuaJiBot.NET.Plugin.MessageBridge.PluginConfig;
using Newtonsoft.Json.Linq;
using CalendarConfig = HuaJiBot.NET.Plugin.Calendar.PluginConfig;
using GithubConfig = HuaJiBot.NET.Plugin.GitHubBridge.PluginConfig;
using RepairConfig = HuaJiBot.NET.Plugin.RepairTeam.PluginConfig;
using SummaryConfig = HuaJiBot.NET.Plugin.DailySummary.Config.PluginConfig;

namespace HuaJiBot.NET.UnitTest;

[NonParallelizable]
internal class PushPanelTest
{
    [Test]
    public void Apply_UpdatesRoutesAndKeepsUnrelatedSecrets()
    {
        using var scope = new ConfigScope();
        var github = new GithubConfig
        {
            BroadcastGroup = new() { ["default"] = ["726156781"] },
            BroadcastMap = new() { ["nbtca/Home"] = "default" },
        };
        var bridge = new BridgeConfig
        {
            Clients =
            [
                new()
                {
                    Address = "wss://mq.nbtca.space/mc",
                    Token = "bridge-secret",
                    Groups = [new() { GroupId = "221064637" }],
                },
            ],
        };
        scope.Wrapper.Populate(PushRules.Github, github);
        scope.Wrapper.Populate(PushRules.Bridge, bridge);
        scope.Root.Plugins["AIChat"] = JObject.Parse("""{"ApiKey":"model-secret"}""");

        var document = PushRules.Read(scope.Wrapper);
        document.Github!.Routes["nbtca/Roadmap"] = "default";
        document.Github.Groups["default"] = ["726156781", "466691612"];
        document.Bridge!.Clients[0].Groups[0].ForwardToClient = false;

        Assert.That(PushRules.Apply(scope.Wrapper, document), Is.Null);

        var saved = JObject.Parse(File.ReadAllText("config.json"));
        Assert.Multiple(() =>
        {
            Assert.That(github.BroadcastMap["nbtca/Roadmap"], Is.EqualTo("default"));
            Assert.That(github.BroadcastGroup["default"], Is.EqualTo(new[] { "726156781", "466691612" }));
            Assert.That(bridge.Clients[0].Token, Is.EqualTo("bridge-secret"));
            Assert.That(bridge.Clients[0].Groups[0].ForwardToClient, Is.False);
            Assert.That(saved["Plugins"]!["AIChat"]!["ApiKey"]!.Value<string>(), Is.EqualTo("model-secret"));
            Assert.That(saved["Plugins"]!["MessageBridge"]!["Clients"]![0]!["Token"]!.Value<string>(), Is.EqualTo("bridge-secret"));
            Assert.That(JsonOf(PushRules.Read(scope.Wrapper)), Does.Not.Contain("bridge-secret"));
            Assert.That(JsonOf(PushRules.Read(scope.Wrapper)), Does.Not.Contain("model-secret"));
        });
    }

    [Test]
    public void Apply_RejectsUnknownGroupWithoutWriting()
    {
        using var scope = new ConfigScope();
        var github = new GithubConfig
        {
            BroadcastGroup = new() { ["default"] = ["1"] },
            BroadcastMap = new() { ["nbtca/Home"] = "default" },
        };
        scope.Wrapper.Populate(PushRules.Github, github);
        var document = PushRules.Read(scope.Wrapper);
        document.Github!.Routes["nbtca/Home"] = "missing";

        Assert.That(PushRules.Apply(scope.Wrapper, document), Does.Contain("不存在的分组"));
        Assert.Multiple(() =>
        {
            Assert.That(github.BroadcastMap["nbtca/Home"], Is.EqualTo("default"));
            Assert.That(File.Exists("config.json"), Is.False);
        });
    }

    [Test]
    public void Apply_RejectsBridgeAddressChange()
    {
        using var scope = new ConfigScope();
        var bridge = new BridgeConfig
        {
            Clients = [new() { Address = "wss://mq.nbtca.space/mc", Token = "keep" }],
        };
        scope.Wrapper.Populate(PushRules.Bridge, bridge);
        var document = PushRules.Read(scope.Wrapper);
        document.Bridge!.Clients[0].Address = "wss://example.invalid/mc";

        Assert.That(PushRules.Apply(scope.Wrapper, document), Does.Contain("地址不能"));
        Assert.That(bridge.Clients[0].Address, Is.EqualTo("wss://mq.nbtca.space/mc"));
        Assert.That(bridge.Clients[0].Token, Is.EqualTo("keep"));
    }

    [Test]
    public void Read_IncludesTheOtherPushRules()
    {
        using var scope = new ConfigScope();
        scope.Wrapper.Populate(
            PushRules.Calendar,
            new CalendarConfig
            {
                ReminderGroups =
                [
                    new()
                    {
                        GroupId = "466691612",
                        Mode = CalendarConfig.ReminderFilterConfig.FilterMode.BlackList,
                        Keywords = [],
                    },
                ],
            }
        );
        scope.Wrapper.Populate(
            PushRules.Repair,
            new RepairConfig { PushInfoGroup = ["875853470"], RemindHour = 12, OpenDays = 1 }
        );
        scope.Wrapper.Populate(PushRules.Summary, new SummaryConfig { GroupIds = ["466691612"], SummaryHour = 0 });

        var document = PushRules.Read(scope.Wrapper);

        Assert.Multiple(() =>
        {
            Assert.That(document.Calendar!.Groups[0].GroupId, Is.EqualTo("466691612"));
            Assert.That(document.Calendar.Groups[0].Mode, Is.EqualTo("BlackList"));
            Assert.That(document.Repair!.PushInfoGroups, Is.EqualTo(new[] { "875853470" }));
            Assert.That(document.Summary!.GroupIds, Is.EqualTo(new[] { "466691612" }));
            Assert.That(document.Github, Is.Null);
        });
    }

    [Test]
    public async Task Panel_RequiresTokenAndSaves()
    {
        using var scope = new ConfigScope();
        var github = new GithubConfig
        {
            BroadcastGroup = new() { ["default"] = ["1"] },
            BroadcastMap = new() { ["nbtca/Home"] = "default" },
        };
        scope.Wrapper.Populate(PushRules.Github, github);
        var port = FreePort();
        using var server = new PanelServer("127.0.0.1", port, "panel-secret", scope.Wrapper);
        server.Start();
        using var http = new HttpClient();

        var page = await http.GetStringAsync($"http://127.0.0.1:{port}/");
        var denied = await http.GetAsync($"http://127.0.0.1:{port}/api/rules");
        http.DefaultRequestHeaders.Authorization = new("Bearer", "panel-secret");
        var body = new StringContent(
            """
            {"github":{"groups":{"default":["1","2"]},"routes":{"nbtca/Home":"default","nbtca/Roadmap":"default"}}}
            """,
            Encoding.UTF8,
            "application/json"
        );
        var saved = await http.PutAsync($"http://127.0.0.1:{port}/api/rules", body);
        var text = await saved.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(page, Does.Contain("推送规则"));
            Assert.That(page, Does.Not.Contain("panel-secret"));
            Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(text, Does.Contain("nbtca/Roadmap"));
            Assert.That(text, Does.Not.Contain("panel-secret"));
            Assert.That(github.BroadcastMap["nbtca/Roadmap"], Is.EqualTo("default"));
            Assert.That(github.BroadcastGroup["default"], Is.EqualTo(new[] { "1", "2" }));
        });
    }

    private static int FreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string JsonOf(PushRulesDocument document) =>
        Newtonsoft.Json.JsonConvert.SerializeObject(document, PushRules.Json);

    private sealed class ConfigScope : IDisposable
    {
        private readonly string _previous = Environment.CurrentDirectory;
        public HuaJiBot.NET.Config.Config Root { get; } = new();
        public ConfigWrapper Wrapper { get; }

        public ConfigScope()
        {
            Wrapper = new ConfigWrapper(Root);
            var dir = Directory.CreateTempSubdirectory("push-panel-").FullName;
            Environment.CurrentDirectory = dir;
        }

        public void Dispose()
        {
            var dir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _previous;
            Directory.Delete(dir, true);
        }
    }
}
