using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

namespace HuaJiBot.NET.Mcp.WebSearch;

[McpServerToolType]
public sealed class WebTools
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(35) };
    private static readonly WebDataService Data = new(
        Client,
        Environment.GetEnvironmentVariable("SEARXNG_URL") ?? "http://127.0.0.1:8088"
    );

    [McpServerTool(Name = "web_search")]
    [Description("搜索网页并返回标题、摘要、网址、来源和检索时间。知识不足、不确定或需要核实当前事实时调用；不要凭印象编造结果。")]
    public static Task<string> SearchWebAsync(
        [Description("搜索关键词，不超过 160 字。")]
            string query
    ) => Data.SearchAsync(query);
}

public sealed class WebDataService(HttpClient client, string searxngBaseUrl)
{
    private readonly Uri _searchBase = new(searxngBaseUrl.TrimEnd('/') + "/");

    public async Task<string> SearchAsync(string query)
    {
        query = Validate(query, "搜索词", 160);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var engines = attempt == 0
                ? "google,google cse,360search,sogou"
                : "google cse,360search";
            var path = "search?format=json&q=" + Uri.EscapeDataString(query)
                + "&engines=" + Uri.EscapeDataString(engines);
            try
            {
                using var response = await client.GetAsync(new Uri(_searchBase, path));
                response.EnsureSuccessStatusCode();
                using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                var text = FormatSearchResults(json.RootElement, query);
                if (text.Length > 0)
                    return $"检索时间：{DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz}\n{text}";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                Console.Error.WriteLine($"网页搜索第 {attempt + 1} 次请求失败：{ex.Message}");
            }
        }
        return "搜索引擎暂时未返回结果，请稍后重试。";
    }

    private static string Validate(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"{name}不能为空且不能超过 {maxLength} 字。", nameof(value));
        return value.Trim();
    }

    public static string FormatSearchResults(JsonElement root, string? query = null)
    {
        if (!root.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array)
            return "";
        var lines = new List<string>();
        var hostCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var identifier = query is null
            ? null
            : Regex.Match(query, @"(?i)(?<![a-z0-9])[a-z]{1,4}\d{2,6}(?![a-z0-9])").Value;
        var digits = identifier is { Length: > 0 }
            ? new string(identifier.Where(char.IsDigit).ToArray())
            : null;
        foreach (var item in results.EnumerateArray())
        {
            if (lines.Count == 5)
                break;
            var url = ReadString(item, "url");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                || parsed.Scheme is not ("http" or "https"))
                continue;
            var title = ReadString(item, "title");
            var content = ReadString(item, "content");
            if (identifier is { Length: > 0 }
                && !title.Contains(identifier, StringComparison.OrdinalIgnoreCase)
                && !url.Contains(identifier, StringComparison.OrdinalIgnoreCase)
                && !content.Contains(identifier, StringComparison.OrdinalIgnoreCase)
                && (digits is null
                    || !(title.Contains(digits, StringComparison.OrdinalIgnoreCase)
                        || url.Contains(digits, StringComparison.OrdinalIgnoreCase))))
                continue;
            var host = parsed.Host;
            if (hostCounts.GetValueOrDefault(host) >= 2)
                continue;
            hostCounts[host] = hostCounts.GetValueOrDefault(host) + 1;
            var engine = ReadString(item, "engine");
            var source = engine.Length == 0 ? "" : $"（{engine}）";
            lines.Add($"{lines.Count + 1}. {title}{source}\n{url}\n{Clip(content, 240)}");
        }
        return string.Join("\n\n", lines);
    }

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
