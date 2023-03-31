using System.IO;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Security.Policy;
using System.Net.Http.Json;
using System.Windows.Shapes;
using System.Text.Json;

namespace BDCloudDrive.Utils;

static class HttpClientUtils
{
    private static HttpClient client = new();

    public static Task<HttpResponseMessage> GetAsync(Uri url, IDictionary<string, string>? headers = null)
    {
        if (headers != null) foreach (var k in headers.Keys)
            {
                client.DefaultRequestHeaders.Add(k, headers[k]);
            }
        return client.GetAsync(url);
    }
    public static Task<HttpResponseMessage> GetAsync(string url, IDictionary<string, string>? headers = null)
    {
        return GetAsync(new Uri(url.Replace("\\", "/")), headers);
    }

    public static async Task<JsonNode?> GetJsonNodeAsync(string url, IDictionary<string, string>? headers = null)
    {
        using HttpResponseMessage response = await GetAsync(url, headers);
        var res = await response.Content.ReadAsStringAsync();
        if (res != null)
        {
            return JsonNode.Parse(res);
        }
        return null;
    }
    public static async Task<JsonNode?> GetJsonNodeAsync(string url)
    {
        return await GetJsonNodeAsync(url, null);
    }

}
