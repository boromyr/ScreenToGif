using ScreenToGif.Domain.Enums;
using ScreenToGif.Util.Settings;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace ScreenToGif.Util;

/// <summary>
/// Web related methods.
/// Boundary: http://www.w3.org/Protocols/rfc1341/7_2_Multipart.html
/// </summary>
public static class WebHelper
{
    private const string Boundary = "+++fringe+++";

    public static string Protect(string str)
    {
        var entropy = Encoding.ASCII.GetBytes(Environment.MachineName);
        return Convert.ToBase64String(ProtectedData.Protect(Encoding.ASCII.GetBytes(str), entropy, DataProtectionScope.CurrentUser));
    }

    public static string Unprotect(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        var entropy = Encoding.ASCII.GetBytes(Environment.MachineName);
        return Encoding.ASCII.GetString(ProtectedData.Unprotect(Convert.FromBase64String(str), entropy, DataProtectionScope.CurrentUser));
    }

    public static string AppendQuery(string url, Dictionary<string, string> args)
    {
        if (args == null)
            return url;

        var suffix = args.Select(s => s.Key + "=" + WebUtility.UrlEncode(s.Value)).Aggregate((p, n) => p + "&" + n);

        return url + (string.IsNullOrWhiteSpace(suffix) ? "" : "?" + suffix);
    }


    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";

    public static Task<string> Get(string url, NameValueCollection headers = null)
    {
        return Send(HttpMethod.Get, url, null, null, headers);
    }

    public static async Task<string> Post(string url, string content, NameValueCollection headers = null)
    {
        await using var stream = new MemoryStream();
        stream.WriteStringUtf8(content);

        return await Send(HttpMethod.Post, url, stream, "application/json", headers);
    }

    public static async Task<string> PostMultipart(string url, Dictionary<string, string> args)
    {
        await using var stream = new MemoryStream();
        stream.WriteStringUtf8(GetMultipartString(Boundary, args));

        return await Send(HttpMethod.Post, url, stream, "multipart/form-data; boundary=" + Boundary);
    }

    public static async Task<string> SendFile(string url, Stream data, string filename, Dictionary<string, string> args = null, NameValueCollection headers = null, string streamName = "file")
    {
        await using var head = GetMultipartStream(Boundary, args, filename, data, streamName);

        return await Send(HttpMethod.Post, url, head, "multipart/form-data; boundary=" + Boundary, headers);
    }

    public static async Task<string> SendFile2(string url, Stream data, string filename, Dictionary<string, string> args = null, NameValueCollection headers = null)
    {
        await using var head = GetMultipartStream(Boundary, args, filename, data);

        return await Send(HttpMethod.Post, url, head, "multipart/form-data; boundary=" + Boundary, headers);
    }

    public static Stream GetMultipartStream(string border, Dictionary<string, string> args, string filename, Stream data, string streamName = "file")
    {
        var stream = new MemoryStream();

        if (args.Any(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value)))
            stream.WriteStringUtf8("Content-Type: text/plain; charset=utf-8");

        foreach (var content in args.Where(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value)))
            stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name=\"{content.Key}\"\r\n\r\n{content.Value}\r\n");

        if (!string.IsNullOrWhiteSpace(filename))
            stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name={streamName}; filename={filename};\r\n\r\n");
        //stream.WriteStringUtf8($"--{border}\r\nContent-Disposition: form-data; name=\"image\"; filename=\"{filename}\"\r\nContent-Type: image/gif\r\n\r\n"); //TODO: Fixed content type.

        stream.WriteStream(data);
            
        stream.WriteStringUtf8($"\r\n--{border}--\r\n");

        stream.Position = 0;
        return stream;
    }

    public static string GetMultipartString(string border, Dictionary<string, string> args)
    {
        return args.Where(w => !string.IsNullOrEmpty(w.Key) && !string.IsNullOrEmpty(w.Value))
            .Aggregate("", (p, n) => p + $"--{border}\r\nContent-Disposition: form-data; name=\"{n.Key}\"\r\n\r\n{n.Value}\r\n") + $"--{border}--\r\n";
    }

    /// <summary>
    /// Sends the request and returns the body of the response, even for unsuccessful status codes (the APIs return the error details in it).
    /// </summary>
    private static async Task<string> Send(HttpMethod method, string url, Stream data, string contentType = null, NameValueCollection headers = null)
    {
        var proxy = GetProxy();
        using var handler = new HttpClientHandler { Proxy = proxy, UseProxy = proxy != null };
        using var client = new HttpClient(handler);

        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        if (headers != null)
            foreach (string key in headers)
                request.Headers.TryAddWithoutValidation(key, headers[key]);

        if (data is { Length: > 0 })
        {
            data.Position = 0;
            request.Content = new StreamContent(data);
            request.Content.Headers.ContentLength = data.Length;

            if (contentType != null)
                request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

            //Uploads can take a while.
            client.Timeout = Timeout.InfiniteTimeSpan;
        }
        else
        {
            request.Headers.ConnectionClose = true;
        }

        using var response = await client.SendAsync(request);

        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<Stream> GetStream(string url, NameValueCollection headers = null)
    {
        using var client = GetHttpClient(headers);
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Unsuccessful download of stream.");

        return await response.Content.ReadAsStreamAsync();
    }

    internal static HttpClient GetHttpClient(NameValueCollection headers = null)
    {
        var clientHandler = new HttpClientHandler { Proxy = GetProxy() };
        var client = new HttpClient(clientHandler);

        if (headers != null)
            foreach (string key in headers)
                client.DefaultRequestHeaders.Add(key, headers[key]);
        
        return client;
    }

    public static IWebProxy GetProxy()
    {
        if (UserSettings.All.ProxyMode == ProxyTypes.System)
            return WebRequest.GetSystemWebProxy();

        if (UserSettings.All.ProxyMode == ProxyTypes.Manual)
            return string.IsNullOrEmpty(UserSettings.All.ProxyHost) || UserSettings.All.ProxyPort <= 0 ? null :
                new WebProxy($"{UserSettings.All.ProxyHost}:{UserSettings.All.ProxyPort}", true, null, new NetworkCredential(UserSettings.All.ProxyUsername, Unprotect(UserSettings.All.ProxyPassword)));

        return null;
    }

    internal static bool IsProxyBeingUsed()
    {
        if (UserSettings.All.ProxyMode == ProxyTypes.System)
            return true;

        if (UserSettings.All.ProxyMode == ProxyTypes.Manual)
            return !string.IsNullOrEmpty(UserSettings.All.ProxyHost) && UserSettings.All.ProxyPort > 0;

        return false;
    }
}