using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace HuaJiBot.NET.Mcp.WebSearch;

[McpServerToolType]
public sealed class WebTools
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly WebDataService Data = new(
        Client,
        Environment.GetEnvironmentVariable("SEARXNG_URL") ?? "http://127.0.0.1:8088"
    );

    [McpServerTool(Name = "web_search")]
    [Description("搜索实时网页信息，返回标题、摘要、原始网址和搜索来源。需要最新事实、新闻、链接时先调用。")]
    public static Task<string> SearchWebAsync(
        [Description("搜索关键词，不超过 160 字。")]
            string query
    ) => Data.SearchAsync(query);

    [McpServerTool(Name = "get_weather")]
    [Description("查询城市的当前天气和今天预报，返回地点、数据时间、气温、降水及数据来源。回答实时天气问题时先调用。")]
    public static Task<string> GetWeatherAsync(
        [Description("城市或地点名称，例如宁波、上海、Beijing。")]
            string location
    ) => Data.WeatherAsync(location);
}

public sealed class WebDataService(HttpClient client, string searxngBaseUrl)
{
    private readonly Uri _searchBase = new(searxngBaseUrl.TrimEnd('/') + "/");

    public async Task<string> SearchAsync(string query)
    {
        query = Validate(query, "搜索词", 160);
        var uri = new Uri(_searchBase, "search?format=json&q=" + Uri.EscapeDataString(query));
        using var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var text = FormatSearchResults(json.RootElement);
        return text.Length == 0 ? "搜索服务没有返回结果，请换个关键词或稍后重试。" : text;
    }

    public async Task<string> WeatherAsync(string location)
    {
        location = Validate(location, "地点", 80);
        var geocodeUrl = "https://geocoding-api.open-meteo.com/v1/search?name="
            + Uri.EscapeDataString(location)
            + "&count=1&language=zh&format=json";
        using var geocode = await client.GetAsync(geocodeUrl);
        geocode.EnsureSuccessStatusCode();
        using var placeJson = JsonDocument.Parse(await geocode.Content.ReadAsStreamAsync());
        if (!placeJson.RootElement.TryGetProperty("results", out var places)
            || places.GetArrayLength() == 0)
            return $"未找到地点“{location}”，请提供更准确的城市名。";

        var place = places[0];
        var latitude = place.GetProperty("latitude").GetDouble();
        var longitude = place.GetProperty("longitude").GetDouble();
        var forecastUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max&forecast_days=1&timezone=auto";
        using var forecast = await client.GetAsync(forecastUrl);
        forecast.EnsureSuccessStatusCode();
        using var weatherJson = JsonDocument.Parse(await forecast.Content.ReadAsStreamAsync());
        return FormatWeather(place, weatherJson.RootElement, forecastUrl);
    }

    private static string Validate(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"{name}不能为空且不能超过 {maxLength} 字。", nameof(value));
        return value.Trim();
    }

    public static string FormatSearchResults(JsonElement root)
    {
        if (!root.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array)
            return "";
        var lines = new List<string>();
        foreach (var item in results.EnumerateArray())
        {
            if (lines.Count == 5)
                break;
            var url = ReadString(item, "url");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                || parsed.Scheme is not ("http" or "https"))
                continue;
            lines.Add($"{lines.Count + 1}. {ReadString(item, "title")}\n{url}\n{Clip(ReadString(item, "content"), 240)}");
        }
        return string.Join("\n\n", lines);
    }

    public static string FormatWeather(JsonElement place, JsonElement forecast, string sourceUrl)
    {
        var current = forecast.GetProperty("current");
        var daily = forecast.GetProperty("daily");
        var name = ReadString(place, "name");
        var region = ReadString(place, "admin1");
        var time = ReadString(current, "time");
        var code = current.GetProperty("weather_code").GetInt32();
        return $"{name}（{region}）天气预报，数据时间 {time}（当地时间）\n"
            + $"当前{DescribeWeather(code)}（天气代码 {code}），气温 {current.GetProperty("temperature_2m")}°C，体感 {current.GetProperty("apparent_temperature")}°C，湿度 {current.GetProperty("relative_humidity_2m")}%，降水 {current.GetProperty("precipitation")} mm，风速 {current.GetProperty("wind_speed_10m")} km/h。\n"
            + $"今日 {daily.GetProperty("temperature_2m_min")[0]}～{daily.GetProperty("temperature_2m_max")[0]}°C，最高降水概率 {daily.GetProperty("precipitation_probability_max")[0]}%。\n"
            + $"来源：Open-Meteo {sourceUrl}";
    }

    private static string DescribeWeather(int code) => code switch
    {
        0 => "晴",
        1 => "大致晴朗",
        2 => "局部多云",
        3 => "阴",
        45 or 48 => "有雾",
        >= 51 and <= 57 => "毛毛雨",
        >= 61 and <= 67 => "雨",
        >= 71 and <= 77 => "雪",
        >= 80 and <= 82 => "阵雨",
        >= 85 and <= 86 => "阵雪",
        >= 95 and <= 99 => "雷雨",
        _ => "天气情况未知",
    };

    private static string ReadString(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static string Clip(string text, int maxLength)
    {
        var singleLine = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine.Length <= maxLength ? singleLine : singleLine[..maxLength] + "…";
    }
}
