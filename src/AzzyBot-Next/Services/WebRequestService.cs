﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Utilities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

public sealed class WebRequestService(ILogger<WebRequestService> logger) : IDisposable
{
    private readonly ILogger _logger = logger;
    private const string MediaType = MediaTypeNames.Application.Json;

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
        },
        PooledConnectionLifetime = TimeSpan.FromMinutes(15)
    })
    {
        DefaultRequestVersion = HttpVersion.Version11,
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Default HttpClient which prefers IPv6.
    /// </summary>
    private readonly HttpClient _httpClient = new(new SocketsHttpHandler()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15)
    })
    {
        DefaultRequestVersion = HttpVersion.Version11,
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(30)
    };

    public void Dispose()
    {
        _httpClientV4?.Dispose();
        _httpClient?.Dispose();
    }

    public async Task<IReadOnlyList<bool>> CheckForApiPermissionsAsync(IReadOnlyList<Uri> urls, Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(urls, nameof(urls));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(urls.Count, nameof(urls));

        List<bool> results = new(urls.Count);
        foreach (Uri url in urls)
        {
            bool success = false;

            try
            {
                HttpResponseMessage? response = null;
                AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);
                AddHeaders(addressFamily, headers, true, true);
                HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;

                response = await client.GetAsync(url);
                success = response.IsSuccessStatusCode;
            }
            catch (InvalidOperationException)
            {
                _logger.WebInvalidUri(url);
                success = false;
            }
            catch (HttpRequestException ex)
            {
                _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);
                success = false;
            }

            results.Add(success);
        }

        return results;
    }

    public async Task DeleteAsync(Uri uri, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(uri);
        AddHeaders(addressFamily, headers, acceptJson, noCache);
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;

        try
        {
            using HttpResponseMessage response = await client.DeleteAsync(uri);
            if (response.IsSuccessStatusCode)
                return;

            _logger.WebRequestFailed(HttpMethod.Delete, response.ReasonPhrase ?? string.Empty, uri);
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(uri);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.WebRequestFailed(HttpMethod.Delete, ex.Message, uri);
            throw;
        }
    }

    public async Task DownloadAsync(Uri url, string downloadPath, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);
        AddHeaders(addressFamily, headers, acceptJson, noCache);
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;

        try
        {
            using HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using Stream contentStream = await response.Content.ReadAsStreamAsync();
            await using FileStream fileStream = new(downloadPath, FileMode.Create, FileAccess.Write);
            await contentStream.CopyToAsync(fileStream);
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);
            throw;
        }
    }

    public async Task<long> GetPingAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri, nameof(uri));

        try
        {
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync(uri.Host, 1000);

            return reply.RoundtripTime;
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(uri);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, uri);
            throw;
        }
    }

    public async Task<string> GetWebAsync(Uri url, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);
        AddHeaders(addressFamily, headers, acceptJson, noCache);
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;

        try
        {
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);
            throw;
        }
    }

    public async Task PostWebAsync(Uri url, string? content = null, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);
        AddHeaders(addressFamily, headers, acceptJson, noCache);
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;

        try
        {
            using HttpContent httpContent = new StringContent(content ?? string.Empty, Encoding.UTF8, MediaType);
            using HttpResponseMessage response = await client.PostAsync(url, httpContent);
            if (response.IsSuccessStatusCode)
                return;

            _logger.WebRequestFailed(HttpMethod.Post, response.ReasonPhrase ?? string.Empty, url);
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.WebRequestFailed(HttpMethod.Post, ex.Message, url);
            throw;
        }
    }

    public async Task PutWebAsync(Uri url, string? content = null, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);
        AddHeaders(addressFamily, headers, acceptJson, noCache);
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;

        try
        {
            using HttpContent httpContent = new StringContent(content ?? string.Empty, Encoding.UTF8, MediaType);
            using HttpResponseMessage response = await client.PutAsync(url, httpContent);
            if (response.IsSuccessStatusCode)
                return;

            _logger.WebRequestFailed(HttpMethod.Put, response.ReasonPhrase ?? string.Empty, url);
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.WebRequestFailed(HttpMethod.Put, ex.Message, url);
            throw;
        }
    }

    private void AddHeaders(AddressFamily addressFamily, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        string botName = AzzyStatsSoftware.GetBotName.Replace("Bot", string.Empty, StringComparison.OrdinalIgnoreCase);
        string botVersion = AzzyStatsSoftware.GetBotVersion;
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new(botName, botVersion));
        if (acceptJson)
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(MediaType));

        if (noCache)
        {
            client.DefaultRequestHeaders.CacheControl = new() { NoCache = true };
            client.DefaultRequestHeaders.Pragma.Add(new("no-cache"));
        }

        if (headers is null)
            return;

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
        foreach (IPAddress _ in iPAddresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6))
        {
            if (await TestIfPreferredMethodIsReachableAsync(url, AddressFamily.InterNetworkV6))
                return AddressFamily.InterNetworkV6;
        }

        return AddressFamily.InterNetwork;
    }

    private static async Task<bool> TestIfPreferredMethodIsReachableAsync(Uri url, AddressFamily addressFamily)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));

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