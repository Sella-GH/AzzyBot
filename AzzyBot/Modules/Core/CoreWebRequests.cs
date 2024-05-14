using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Logging;

namespace AzzyBot.Modules.Core;

internal static class CoreWebRequests
{
    /// <summary>
    /// Forcing this client to use IPv4, only TCP ports because AzuraCast prefers TCP
    /// </summary>
    private static readonly HttpClient ClientV4 = new(new SocketsHttpHandler()
    {
        ConnectCallback = async (context, cancellationToken) =>
        {
            Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
            return new NetworkStream(socket, true);
        }
    })
    {
        DefaultRequestVersion = new(1, 1),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(15)
    };

    /// <summary>
    /// General HttpClient which prefers IPv6
    /// </summary>
    private static readonly HttpClient Client = new()
    {
        DefaultRequestVersion = new(1, 1),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(30)
    };

    internal static HttpClient GetHttpClientV4(Dictionary<string, string> headers)
    {
        if (headers.Count is 0)
            return ClientV4;

        AddHeaders(headers, false);

        return ClientV4;
    }

    internal static HttpClient GetHttpClient(Dictionary<string, string> headers)
    {
        if (headers.Count is 0)
            return Client;

        AddHeaders(headers);

        return Client;
    }

    internal static async Task<string> GetWebAsync(string url, Dictionary<string, string>? headers = null, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers, ipv6);

            HttpClient client = (ipv6) ? Client : ClientV4;
            string content;

            using (HttpResponseMessage response = await client.GetAsync(new Uri(url)))
            {
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
            }

            if (content.Contains("You must be logged in to access this page.", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Your AzuraCast API key is wrong!");

            return content;
        }
        catch (HttpRequestException e)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, $"GET request for {url} failed with error {e.Message}", null);
            throw;
        }
    }

    internal static async Task<HttpResponseMessage> GetWebDownloadAsync(string url, Dictionary<string, string> headers, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            AddHeaders(headers, ipv6);

            HttpClient client = (ipv6) ? Client : ClientV4;
            HttpResponseMessage response = await client.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();

            return response;
        }
        catch (HttpRequestException e)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, $"GET request for {url} failed with error {e.Message}", null);
            throw;
        }
    }

    internal static async Task<bool> PostWebAsync(string url, string content = "", Dictionary<string, string>? headers = null, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers, ipv6);

            HttpResponseMessage? response;
            HttpClient client = (ipv6) ? Client : ClientV4;
            HttpContent? httpContent = null;

            if (!string.IsNullOrWhiteSpace(content))
                httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            response = await client.PostAsync(new Uri(url), httpContent);

            if (response.StatusCode != HttpStatusCode.BadRequest && response.StatusCode != HttpStatusCode.InternalServerError && url.Contains(CoreModule.GetAzuraCastApiUrl(), StringComparison.OrdinalIgnoreCase))
            {
                response.EnsureSuccessStatusCode();
                response.Dispose();
                response = null;
                httpContent?.Dispose();

                return true;
            }

            httpContent?.Dispose();

            return false;
        }
        catch (HttpRequestException e)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, $"POST request for {url} failed with error {e.Message}", null);
            throw;
        }
    }

    internal static async Task<bool> PutWebAsync(string url, string content = "", Dictionary<string, string>? headers = null, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers, ipv6);

            HttpResponseMessage? response;
            HttpClient client = (ipv6) ? Client : ClientV4;
            HttpContent? httpContent = null;

            if (!string.IsNullOrWhiteSpace(content))
                httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            response = await client.PutAsync(new Uri(url), httpContent);
            response.EnsureSuccessStatusCode();
            response.Dispose();
            response = null;
            httpContent?.Dispose();

            return true;
        }
        catch (HttpRequestException e)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, $"PUT request for {url} failed with error {e.Message}", null);
            throw;
        }
    }

    private static void AddHeaders(Dictionary<string, string> headers, bool ipv6 = true)
    {
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));

        HttpClient client = (ipv6) ? Client : ClientV4;
        client.DefaultRequestHeaders.Clear();

        foreach (KeyValuePair<string, string> header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    internal static async Task<string> GetPingTimeAsync(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        string result = string.Empty;
        try
        {
            bool isHttps = url.StartsWith("https", StringComparison.OrdinalIgnoreCase);
            string host = new Uri(url).Host;
            bool isIPaddress = IPAddress.TryParse(host, out IPAddress? iPAddress);

            if (CoreModule.GetAzuracastIPv6Availability())
            {
                if (isIPaddress && iPAddress?.AddressFamily is not AddressFamily.InterNetworkV6)
                {
                    LoggerBase.LogWarn(LoggerBase.GetLogger, "Host address is no IPv6 address! But according to settings it should", null);
                    result = "down";
                }

                if (string.IsNullOrWhiteSpace(result))
                {
                    if (!await CheckLocalConnectionAsync(AddressFamily.InterNetworkV6))
                    {
                        LoggerBase.LogWarn(LoggerBase.GetLogger, "IPv6 is down!", null);
                    }
                    else
                    {
                        result = await CheckServerConnectionAsync(host, AddressFamily.InterNetworkV6, isIPaddress, isHttps);
                        if (string.IsNullOrWhiteSpace(result))
                            LoggerBase.LogWarn(LoggerBase.GetLogger, "Server not reachable over IPv6", null);
                    }
                }
            }

            if (!await CheckLocalConnectionAsync(AddressFamily.InterNetwork))
            {
                LoggerBase.LogError(LoggerBase.GetLogger, "IPv4 is down!", null);
            }
            else if (string.IsNullOrWhiteSpace(result))
            {
                result = await CheckServerConnectionAsync(host, AddressFamily.InterNetwork, isIPaddress, isHttps);
                if (string.IsNullOrWhiteSpace(result))
                    LoggerBase.LogError(LoggerBase.GetLogger, "Server not reachable over IPv4", null);
            }

            if (string.IsNullOrWhiteSpace(result))
                LoggerBase.LogCrit(LoggerBase.GetLogger, "No internet connection available!", null);
        }
        catch (HttpRequestException ex)
        {
            await LoggerExceptions.LogErrorAsync(ex);
        }

        return result;
    }

    private static async Task<bool> CheckLocalConnectionAsync(AddressFamily family)
    {
        try
        {
            // Choose the right address based on the family
            HttpClient client = (family == AddressFamily.InterNetworkV6) ? Client : ClientV4;
            string host = (family == AddressFamily.InterNetworkV6) ? "[2a01:4f8:0:a232::2]" : "78.46.170.2";
            using HttpResponseMessage? response = await client.GetAsync(new Uri($"http://{host}"));

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            await LoggerExceptions.LogErrorAsync(ex);
        }

        return false;
    }

    private static async Task<string> CheckServerConnectionAsync(string url, AddressFamily family, bool isIpAddress, bool isHttps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            // Check if the provided url is a IP address or a domain
            IPAddress[] addresses = (isIpAddress) ? [IPAddress.Parse(url)] : [];
            IPAddress address = IPAddress.Parse("0.0.0.0");
            string addr = string.Empty;

            if (isIpAddress)
            {
                foreach (IPAddress ipAdr in addresses)
                {
                    if (ipAdr.AddressFamily == family)
                    {
                        address = ipAdr;
                        break;
                    }
                }

                if (address.ToString() is "0.0.0.0" or "::0")
                    throw new InvalidOperationException($"{nameof(address)} is zero!");

                addr = (family == AddressFamily.InterNetworkV6) ? $"[{address}]" : address.ToString();
            }

            // Choose between IP Address or Domain and protocol
            HttpClient client = (family == AddressFamily.InterNetworkV6) ? Client : ClientV4;
            string protocol = (isHttps) ? "https://" : "http://";
            Uri finalUrl = new(protocol + ((isIpAddress) ? addr : url));

            // Stop the roundtrip time
            Stopwatch time = Stopwatch.StartNew();
            using HttpResponseMessage? response = await client.GetAsync(finalUrl);
            time.Stop();

            response.EnsureSuccessStatusCode();

            return time.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
        }
        catch (HttpRequestException ex)
        {
            await LoggerExceptions.LogErrorAsync(ex);
        }

        return string.Empty;
    }
}
