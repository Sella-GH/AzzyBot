using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using Microsoft.Extensions.Logging;

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

    private static readonly HttpClient Client = new()
    {
        DefaultRequestVersion = new(1, 1),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(15)
    };

    internal static async Task<string> GetWebAsync(string url, Dictionary<string, string>? headers = null, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers);

            HttpClient client = (ipv6) ? Client : ClientV4;
            string content;

            using (HttpResponseMessage response = await client.GetAsync(new Uri(url)))
            {
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
            }

            if (content.Contains("You must be logged in to access this page.", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Either you forgot your AzuraCast API key or the API key is wrong!");

            return content;
        }
        catch (HttpRequestException e)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, $"GET request for {url} failed with error {e.Message}");
            throw;
        }
    }

    internal static async Task<Stream> GetWebDownloadAsync(string url, Dictionary<string, string> headers, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            AddHeaders(headers);

            HttpClient client = (ipv6) ? Client : ClientV4;
            HttpResponseMessage response = await client.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }
        catch (HttpRequestException e)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, $"GET request for {url} failed with error {e.Message}");
            throw;
        }
    }

    internal static async Task<bool> PostWebAsync(string url, string content = "", Dictionary<string, string>? headers = null, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers);

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
            ExceptionHandler.LogMessage(LogLevel.Error, $"POST request for {url} failed with error {e.Message}");
            throw;
        }
    }

    internal static async Task<bool> PutWebAsync(string url, string content = "", Dictionary<string, string>? headers = null, bool ipv6 = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers);

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
            ExceptionHandler.LogMessage(LogLevel.Error, $"PUT request for {url} failed with error {e.Message}");
            throw;
        }
    }

    private static void AddHeaders(Dictionary<string, string> headers)
    {
        //
        // client.DefaultRequestHeaders.Clear() is greatly neccessary!
        // Otherwise the Headers just add up and up and up
        //

        ArgumentNullException.ThrowIfNull(headers, nameof(headers));

        Client.DefaultRequestHeaders.Clear();

        foreach (KeyValuePair<string, string> header in headers)
        {
            Client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    internal static async Task<string> TryPingAsync(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        string result = string.Empty;
        try
        {
            string host = new Uri(url).Host;
            bool isIPaddress = IPAddress.TryParse(host, out IPAddress? iPAddress);

            if (CoreModule.GetAzuracastIPv6Availability())
            {
                if (isIPaddress && iPAddress?.AddressFamily is not AddressFamily.InterNetworkV6)
                {
                    ExceptionHandler.LogMessage(LogLevel.Warning, "Host address is no IPv6 address!");
                    result = "down";
                }

                if (string.IsNullOrWhiteSpace(result))
                {
                    if (!await CheckLocalConnectionAsync(AddressFamily.InterNetworkV6))
                    {
                        ExceptionHandler.LogMessage(LogLevel.Warning, "IPv6 is down!");
                    }
                    else
                    {
                        result = await PingServerAsync(host, AddressFamily.InterNetworkV6, isIPaddress);
                        if (string.IsNullOrWhiteSpace(result))
                            ExceptionHandler.LogMessage(LogLevel.Debug, "Server not reachable over IPv6");
                    }
                }
            }

            if (!await CheckLocalConnectionAsync(AddressFamily.InterNetwork))
            {
                ExceptionHandler.LogMessage(LogLevel.Warning, "IPv4 is down!");
            }
            else if (string.IsNullOrWhiteSpace(result))
            {
                result = await PingServerAsync(host, AddressFamily.InterNetwork, isIPaddress);
                if (string.IsNullOrWhiteSpace(result))
                    ExceptionHandler.LogMessage(LogLevel.Debug, "Server not reachable over IPv4");
            }

            if (string.IsNullOrWhiteSpace(result))
                ExceptionHandler.LogMessage(LogLevel.Critical, "No internet connection available!");
        }
        catch (HttpRequestException)
        { }

        return result;
    }

    private static async Task<bool> CheckLocalConnectionAsync(AddressFamily family)
    {
        try
        {
            // Choose the right address based on the family
            string host = (family == AddressFamily.InterNetworkV6) ? "2a01:4f8:0:a232::2" : "78.46.170.2";

            using Ping? ping = new();
            PingReply reply = await ping.SendPingAsync(host);

            return reply.Status == IPStatus.Success;
        }
        catch (HttpRequestException)
        { }

        return false;
    }

    private static async Task<string> PingServerAsync(string url, AddressFamily family, bool isIpAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            // Check if the provided url is a IP address or a domain
            IPAddress[] addresses = (isIpAddress) ? [IPAddress.Parse(url)] : await Dns.GetHostAddressesAsync(url);
            IPAddress address = IPAddress.Parse("0.0.0.0");

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

            using Ping? ping = new();
            PingReply reply = await ping.SendPingAsync(address);

            if (reply.Status == IPStatus.Success)
                return reply.RoundtripTime.ToString(CultureInfo.InvariantCulture);
        }
        catch (HttpRequestException)
        { }

        return string.Empty;
    }
}
