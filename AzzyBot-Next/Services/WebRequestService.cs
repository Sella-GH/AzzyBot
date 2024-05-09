using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Utilities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal sealed class WebRequestService(ILogger<WebRequestService> logger) : IDisposable
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Forcing this client to use IPv4, only TCP ports because HTTP and HTTPS are usually TCP.
    /// </summary>
    private readonly HttpClient _httpClientV4 = new(new SocketsHttpHandler()
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
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Default HttpClient which prefers IPv6.
    /// </summary>
    private readonly HttpClient _httpClient = new()
    {
        DefaultRequestVersion = new(1, 1),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(30)
    };

    public void Dispose()
    {
        _httpClientV4?.Dispose();
        _httpClient?.Dispose();
    }

    internal async Task<string> GetWebAsync(Uri url, Dictionary<string, string>? headers = null)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);

        if (headers is not null)
            AddHeaders(headers, addressFamily);

        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;
        string content;

        try
        {
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
            }

            return content;
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.WebRequestFailed("GET", ex.Message);
            throw;
        }
    }

    private void AddHeaders(Dictionary<string, string> headers, AddressFamily addressFamily)
    {
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));

        string botName = AzzyStatsGeneral.GetBotName;
        string botVersion = AzzyStatsGeneral.GetBotVersion;
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new(botName, botVersion));

        foreach (KeyValuePair<string, string> header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    private static async Task<AddressFamily> GetPreferredIpMethodAsync(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));

        // First check if the host is an IP address
        bool isIpAddress = false;
        if (IPAddress.TryParse(url.Host, out IPAddress? ipAddress))
            isIpAddress = true;

        // If it's an IP address, we can skip the DNS lookup
        IPAddress[] iPAddresses = (isIpAddress) ? [ipAddress ?? IPAddress.Parse(url.Host)] : await Dns.GetHostAddressesAsync(url.Host);

        if (iPAddresses.Length == 1)
            return iPAddresses[0].AddressFamily;

        // If we have multiple addresses, we need to determine which one to use
        // Prefer IPv6 over IPv4
        foreach (IPAddress address in iPAddresses)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6 && await TestIfPreferredMethodIsReachableAsync(url, AddressFamily.InterNetworkV6))
                return AddressFamily.InterNetworkV6;
        }

        return AddressFamily.InterNetwork;
    }

    private static async Task<bool> TestIfPreferredMethodIsReachableAsync(Uri url, AddressFamily addressFamily)
    {
        ArgumentNullException.ThrowIfNull(url, nameof (url));

        try
        {
            // Test if the host is reachable
            using (Socket socket = new(addressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                // Timeout after 5 seconds
                using (CancellationTokenSource cts = new(TimeSpan.FromSeconds(5)))
                {
                    await socket.ConnectAsync(url.Host, 80, cts.Token);
                }

                return true;
            }
        }
        catch (Exception ex) when (ex is SocketException || ex is OperationCanceledException)
        {
            return false;
        }
    }
}
