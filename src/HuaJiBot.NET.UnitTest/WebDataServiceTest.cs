using System.Net;
using System.Text.Json;
using HuaJiBot.NET.Mcp.WebSearch;
using ModelContextProtocol.Client;

namespace HuaJiBot.NET.UnitTest;

internal class WebDataServiceTest
{
    [Test]
    public async Task McpServerExposesOnlyGenericSearchTool()
    {
        var serverPath = Path.Combine(
            AppContext.BaseDirectory,
            "HuaJiBot.NET.Mcp.WebSearch.dll"
        );
        Assert.That(File.Exists(serverPath), Is.True);
        var transport = new StdioClientTransport(new()
        {
            Command = "dotnet",
            Arguments = [serverPath],
            Name = "web-search-test",
        });
        await using var mcp = await McpClientFactory.CreateAsync(transport);

        var names = (await mcp.ListToolsAsync()).Select(tool => tool.Name).ToArray();
        Assert.That(names, Is.EquivalentTo(new[] { "web_search" }));
    }

    [Test]
    public void SearchResultsIncludeSourcesAndExcludeUnsafeUrls()
    {
        using var json = JsonDocument.Parse(
            """
            {"results":[
              {"title":"官方公告","url":"https://example.org/news","content":"第一段  第二段","engine":"example"},
              {"title":"无效链接","url":"javascript:alert(1)","content":"不应显示"}
            ]}
            """
        );

        var result = WebDataService.FormatSearchResults(json.RootElement);

        Assert.That(result, Does.Contain("官方公告"));
        Assert.That(result, Does.Contain("https://example.org/news"));
        Assert.That(result, Does.Not.Contain("javascript:"));
    }

    [Test]
    public void SearchResultsPreferMatchingTrainNumberAndDifferentHosts()
    {
        using var json = JsonDocument.Parse("""
            {"results":[
              {"title":"Google 登录","url":"https://accounts.google.com/login","content":"无关","engine":"bing"},
              {"title":"D2294 时刻表 A","url":"https://example.com/a","content":"车次","engine":"360search"},
              {"title":"D2294 时刻表 B","url":"https://example.com/b","content":"车次","engine":"360search"},
              {"title":"D2294 时刻表 C","url":"https://example.com/c","content":"车次","engine":"360search"},
              {"title":"D2294 官方查询","url":"https://12306.cn/train","content":"车次","engine":"google cse"}
            ]}
            """);

        var result = WebDataService.FormatSearchResults(json.RootElement, "查询车次 D2294");

        Assert.That(result, Does.Not.Contain("Google 登录"));
        Assert.That(result, Does.Contain("D2294 官方查询"));
        Assert.That(result, Does.Contain("（google cse）"));
        Assert.That(result, Does.Not.Contain("D2294 时刻表 C"));
    }

    [Test]
    public async Task SearchUsesConfiguredSearxngEndpoint()
    {
        Uri? requested = null;
        using var client = new HttpClient(new FakeHandler(request =>
        {
            requested = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"天气\",\"url\":\"https://example.org/weather\",\"content\":\"预报\"}]}"),
            };
        }));

        var result = await new WebDataService(client, "http://searxng:8080").SearchAsync("宁波天气");

        Assert.That(requested!.Host, Is.EqualTo("searxng"));
        Assert.That(requested.Query, Does.Contain("format=json"));
        Assert.That(requested.Query, Does.Contain("google"));
        Assert.That(requested.Query, Does.Not.Contain("bing"));
        Assert.That(result, Does.Contain("https://example.org/weather"));
    }

    [Test]
    public async Task SearchRetriesWithAlternateEnginesWhenFirstSearchIsEmpty()
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new FakeHandler(request =>
        {
            requests.Add(request.RequestUri!);
            var json = requests.Count == 1
                ? "{\"results\":[]}"
                : "{\"results\":[{\"title\":\"仓库\",\"url\":\"https://github.com/nbtca/HuaJiBot.NET\",\"content\":\"项目\"}]}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json),
            };
        }));

        var result = await new WebDataService(client, "http://127.0.0.1:8088").SearchAsync("开源 Bot 教程");

        Assert.That(requests, Has.Count.EqualTo(2));
        Assert.That(requests[1].Query, Does.Contain("engines="));
        Assert.That(result, Does.Contain("https://github.com/nbtca/HuaJiBot.NET"));
    }

    [Test]
    public async Task SearchRetriesAfterAnEngineRequestFails()
    {
        var requestCount = 0;
        using var client = new HttpClient(new FakeHandler(_ =>
        {
            requestCount++;
            return requestCount == 1
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"results":[{"title":"资料","url":"https://example.org/fact","content":"内容"}]}
                        """),
                };
        }));

        var result = await new WebDataService(client, "http://searxng:8080")
            .SearchAsync("不熟悉的事实");

        Assert.That(requestCount, Is.EqualTo(2));
        Assert.That(result, Does.Contain("https://example.org/fact"));
    }

    [TestCase("查询车次 D2294")]
    [TestCase("今天天气如何")]
    [TestCase("HuaJiBot.NET GitHub 仓库")]
    [TestCase("量子纠错的表面码原理")]
    public async Task AnyTopicUsesTheSameWebSearchEndpoint(string query)
    {
        Uri? requested = null;
        using var client = new HttpClient(new FakeHandler(request =>
        {
            requested = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"results":[{"title":"结果 D2294","url":"https://example.org/result","content":"查询结果"}]}
                    """),
            };
        }));

        var result = await new WebDataService(client, "http://searxng:8080")
            .SearchAsync(query);

        Assert.That(requested!.Host, Is.EqualTo("searxng"));
        Assert.That(Uri.UnescapeDataString(requested.Query), Does.Contain(query));
        Assert.That(result, Does.Contain("https://example.org/result"));
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> reply)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(reply(request));
    }
}
