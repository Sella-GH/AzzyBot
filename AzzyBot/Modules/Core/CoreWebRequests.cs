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
    private static readonly HttpClient Client = new()
    {
        DefaultRequestVersion = new(2, 0),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(15)
    };

    internal static async Task<string> GetWebAsync(string url, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers);

            string content;
            using (HttpResponseMessage? response = await Client.GetAsync(new Uri(url)))
            {
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
            }

            return content;
        }
        catch (HttpRequestException e)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, $"GET request for {url} failed with error {e.Message}");
            throw;
        }
    }

    internal static async Task<Stream> GetWebDownloadAsync(string url, Dictionary<string, string> headers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            AddHeaders(headers);

            HttpResponseMessage? response = await Client.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        catch (HttpRequestException e)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, $"GET request for {url} failed with error {e.Message}");
            throw;
        }
    }

    internal static async Task<bool> PostWebAsync(string url, string content = "", Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers);

            HttpResponseMessage? response = null;
            if (!string.IsNullOrWhiteSpace(content))
            {
                using HttpContent httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                response = await Client.PostAsync(new Uri(url), httpContent);
            }
            else
            {
                response = await Client.PostAsync(new Uri(url), null);
            }

            if (response.StatusCode != HttpStatusCode.BadRequest && url.Contains(CoreModule.GetAzuraCastApiUrl(), StringComparison.OrdinalIgnoreCase))
            {
                response.EnsureSuccessStatusCode();
                response.Dispose();
                response = null;

                return true;
            }

            return false;
        }
        catch (HttpRequestException e)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, $"POST request for {url} failed with error {e.Message}");
            throw;
        }
    }

    internal static async Task<bool> PutWebAsync(string url, string content = "", Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            if (headers is not null)
                AddHeaders(headers);

            HttpResponseMessage? response = null;
            if (!string.IsNullOrWhiteSpace(content))
            {
                using HttpContent httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                response = await Client.PutAsync(new Uri(url), httpContent);
            }
            else
            {
                response = await Client.PutAsync(new Uri(url), null);
            }

            response.EnsureSuccessStatusCode();
            response.Dispose();
            response = null;

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

        try
        {
            // Cut the host to the straight point
            string host = new Uri(url).Host;

            if (!await CheckLocalConnectionAsync(AddressFamily.InterNetworkV6))
            {
                ExceptionHandler.LogMessage(LogLevel.Warning, "IPv6 is down!");
            }
            else
            {
                return await PingServerAsync(host);
            }

            if (!await CheckLocalConnectionAsync(AddressFamily.InterNetwork))
            {
                ExceptionHandler.LogMessage(LogLevel.Warning, "IPv4 is down!");
            }
            else
            {
                return await PingServerAsync(host);
            }

            ExceptionHandler.LogMessage(LogLevel.Warning, "No internet connection available!");
        }
        catch (HttpRequestException)
        { }

        return string.Empty;
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

    private static async Task<string> PingServerAsync(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        try
        {
            // Get the correct ip address for the correct protocol via the host url
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(url);
            IPAddress address = IPAddress.Parse("0.0.0.0");

            foreach (IPAddress ipAdr in addresses)
            {
                if (ipAdr.AddressFamily is AddressFamily.InterNetworkV6 or AddressFamily.InterNetwork)
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
