using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HuaJiBot.NET.Config;
using Newtonsoft.Json;

namespace HuaJiBot.NET.Plugin.PushPanel;

internal sealed class PanelServer : IDisposable
{
    private const int MaxBodyChars = 256 * 1024;
    private readonly HttpListener _listener = new();
    private readonly string _token;
    private readonly ConfigWrapper _config;
    private readonly string _page;
    private readonly CancellationTokenSource _cts = new();

    public PanelServer(string host, int port, string token, ConfigWrapper config)
    {
        _token = token;
        _config = config;
        _page = ReadPage();
        _listener.Prefixes.Add($"http://{host}:{port}/");
    }

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(ListenAsync);
    }

    private async Task ListenAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (ObjectDisposedException) when (_cts.IsCancellationRequested)
            {
                return;
            }
            catch (HttpListenerException) when (_cts.IsCancellationRequested)
            {
                return;
            }
            _ = Task.Run(() => HandleAsync(context));
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var handler = SelectHandler(request.Url?.AbsolutePath ?? "/", request.HttpMethod);
            if (handler is null)
            {
                await WriteJson(context.Response, 404, new { error = "没有这个地址" });
                return;
            }
            await handler(context);
        }
        catch (Exception)
        {
            try
            {
                await WriteJson(context.Response, 500, new { error = "面板内部出错" });
            }
            catch (Exception)
            {
                context.Response.Abort();
            }
        }
    }

    private Func<HttpListenerContext, Task>? SelectHandler(string path, string method)
    {
        if (method == "GET" && path is "/" or "/index.html")
            return ServePage;
        if (path != "/api/rules")
            return null;
        return method switch
        {
            "GET" => ServeRules,
            "PUT" => SaveRules,
            _ => MethodNotAllowed,
        };
    }

    private Task ServePage(HttpListenerContext context) =>
        WriteText(context.Response, 200, "text/html; charset=utf-8", _page);

    private async Task ServeRules(HttpListenerContext context)
    {
        if (!Authorized(context.Request))
        {
            await WriteJson(context.Response, 401, new { error = "口令不对" });
            return;
        }
        await WriteJson(context.Response, 200, PushRules.Read(_config));
    }

    private async Task SaveRules(HttpListenerContext context)
    {
        if (!Authorized(context.Request))
        {
            await WriteJson(context.Response, 401, new { error = "口令不对" });
            return;
        }
        var body = await ReadBody(context.Request);
        if (body.Length > MaxBodyChars)
        {
            await WriteJson(context.Response, 413, new { error = "请求过大" });
            return;
        }
        PushRulesDocument? document;
        try
        {
            document = JsonConvert.DeserializeObject<PushRulesDocument>(body, PushRules.Json);
        }
        catch (JsonException)
        {
            await WriteJson(context.Response, 400, new { error = "请求不是规则 JSON" });
            return;
        }
        if (document is null)
        {
            await WriteJson(context.Response, 400, new { error = "请求是空的" });
            return;
        }
        var error = PushRules.Apply(_config, document);
        if (error is not null)
        {
            await WriteJson(context.Response, 400, new { error });
            return;
        }
        await WriteJson(context.Response, 200, PushRules.Read(_config));
    }

    private Task MethodNotAllowed(HttpListenerContext context) =>
        WriteJson(context.Response, 405, new { error = "只接受读取和保存" });

    private bool Authorized(HttpListenerRequest request)
    {
        var header = request.Headers["Authorization"];
        const string prefix = "Bearer ";
        if (header is null || !header.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        var presented = Encoding.UTF8.GetBytes(header[prefix.Length..]);
        var expected = Encoding.UTF8.GetBytes(_token);
        return presented.Length == expected.Length
            && CryptographicOperations.FixedTimeEquals(presented, expected);
    }

    private static async Task<string> ReadBody(HttpListenerRequest request)
    {
        if (request.ContentLength64 > MaxBodyChars)
            return new string(' ', MaxBodyChars + 1);
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task WriteJson(HttpListenerResponse response, int status, object body)
    {
        var json = JsonConvert.SerializeObject(body, PushRules.Json);
        await WriteText(response, status, "application/json; charset=utf-8", json);
    }

    private static async Task WriteText(
        HttpListenerResponse response,
        int status,
        string contentType,
        string text
    )
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.StatusCode = status;
        response.ContentType = contentType;
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private static string ReadPage()
    {
        var name = typeof(PanelServer).Assembly.GetManifestResourceNames().Single(resource =>
            resource.EndsWith("panel.html", StringComparison.Ordinal)
        );
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Close();
        _cts.Dispose();
    }
}
