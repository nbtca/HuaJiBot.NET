using System.Net.Http.Headers;
using System.Text;

namespace HuaJiBot.NET.Adapter.Red;

internal partial class Connector
{
    private readonly string _httpServerUrl = $"http://{url}/api/";

    #region SendMessage
    internal async Task<string?> HttpGetRequest(string node)
    {
        using var httpClient = new HttpClient();
        try
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                authorizationToken
            );
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            var response = await httpClient.GetAsync(_httpServerUrl + node);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }
        catch (Exception e)
        {
            api.LogError("Http请求出错!请求失败/请求超时", e);
        }
        return null;
    }

    internal async Task<string?> HttpPostRequest(string content, string node)
    {
        using var httpClient = new HttpClient();
        try
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                authorizationToken
            );
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            var postContent = new StringContent(content, Encoding.UTF8);
            var response = await httpClient.PostAsync(_httpServerUrl + node, postContent);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }
        catch (Exception e)
        {
            api.LogError("HttpPost请求出错!请求失败/请求超时", e);
        }
        return null;
    }

    internal async Task<string?> HttpPostUpload(string filePath, string node)
    {
        using var httpClient = new HttpClient();
        try
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                authorizationToken
            );
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            using var formData = new MultipartFormDataContent();
            await using var fs = File.OpenRead(filePath);
            formData.Add(new StreamContent(fs), "file", Path.GetFileName(filePath));
            var response = await httpClient.PostAsync(_httpServerUrl + node, formData);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }
        catch (Exception e)
        {
            api.LogError("HttpUpload请求出错!请求失败/请求超时", e);
        }
        return null;
    }
    #endregion
}
