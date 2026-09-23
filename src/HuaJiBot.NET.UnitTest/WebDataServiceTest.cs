using System.Net;
using System.Text.Json;
using HuaJiBot.NET.Mcp.WebSearch;

namespace HuaJiBot.NET.UnitTest;

internal class WebDataServiceTest
{
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

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> reply)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(reply(request));
    }
}
