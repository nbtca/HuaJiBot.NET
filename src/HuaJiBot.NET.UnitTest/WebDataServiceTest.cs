using System.Net;
using System.Text.Json;
using HuaJiBot.NET.Mcp.WebSearch;
using ModelContextProtocol.Client;

namespace HuaJiBot.NET.UnitTest;

internal class WebDataServiceTest
{
    [Test]
    public async Task McpServerExposesSearchAndWeatherTools()
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
        Assert.That(names, Is.EquivalentTo(new[] { "web_search", "get_weather" }));
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
    public async Task WeatherQueriesGeocodingThenForecastAndReportsDataTime()
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new FakeHandler(request =>
        {
            requests.Add(request.RequestUri!);
            var json = request.RequestUri!.Host.StartsWith("geocoding", StringComparison.Ordinal)
                ? """
                  {"results":[{"name":"宁波","admin1":"浙江","latitude":29.87819,"longitude":121.54945}]}
                  """
                : """
                  {"current":{"time":"2026-09-23T14:00","temperature_2m":25.1,"apparent_temperature":27.0,"relative_humidity_2m":70,"precipitation":0.0,"weather_code":3,"wind_speed_10m":8.2},"daily":{"temperature_2m_max":[28.0],"temperature_2m_min":[21.0],"precipitation_probability_max":[30]}}
                  """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json),
            };
        }));

        var result = await new WebDataService(client, "http://searxng:8080").WeatherAsync("宁波");

        Assert.That(requests, Has.Count.EqualTo(2));
        Assert.That(result, Does.Contain("2026-09-23T14:00"));
        Assert.That(result, Does.Contain("25.1°C"));
        Assert.That(result, Does.Contain("https://api.open-meteo.com/v1/forecast"));
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

        var result = await new WebDataService(client, "http://127.0.0.1:8088").SearchAsync("HuaJiBot.NET");

        Assert.That(requests, Has.Count.EqualTo(2));
        Assert.That(requests[1].Query, Does.Contain("engines="));
        Assert.That(result, Does.Contain("https://github.com/nbtca/HuaJiBot.NET"));
    }

    [Test]
    public async Task GitHubRepositoryQueryUsesOfficialApiBeforeSearxng()
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new FakeHandler(request =>
        {
            requests.Add(request.RequestUri!);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"items":[{"full_name":"nbtca/HuaJiBot.NET","html_url":"https://github.com/nbtca/HuaJiBot.NET","description":"Bot"}]}
                    """),
            };
        }));

        var result = await new WebDataService(client, "http://127.0.0.1:8088")
            .SearchAsync("HuaJiBot.NET GitHub 仓库");

        Assert.That(requests, Has.Count.EqualTo(1));
        Assert.That(requests[0].Host, Is.EqualTo("api.github.com"));
        Assert.That(requests[0].Query, Does.Contain("HuaJiBot.NET"));
        Assert.That(result, Does.Contain("https://github.com/nbtca/HuaJiBot.NET"));
    }

    [Test]
    public async Task GitHubApiFailureFallsBackToSearxng()
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new FakeHandler(request =>
        {
            requests.Add(request.RequestUri!);
            return request.RequestUri!.Host == "api.github.com"
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"results":[{"title":"仓库","url":"https://github.com/nbtca/HuaJiBot.NET","content":"项目"}]}
                        """),
                };
        }));

        var result = await new WebDataService(client, "http://127.0.0.1:8088")
            .SearchAsync("HuaJiBot.NET GitHub 仓库");

        Assert.That(requests.Select(uri => uri.Host), Is.EqualTo(new[] { "api.github.com", "127.0.0.1" }));
        Assert.That(result, Does.Contain("https://github.com/nbtca/HuaJiBot.NET"));
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
