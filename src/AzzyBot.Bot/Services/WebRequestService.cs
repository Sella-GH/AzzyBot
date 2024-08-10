using System;
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class WebRequestService(ILogger<WebRequestService> logger) : IDisposable
{
    private readonly ILogger _logger = logger;
    private const string MediaType = MediaTypeNames.Application.Json;

    /// <summary>
    /// Forcing this client to use IPv4, only TCP ports because HTTP and HTTPS are usually TCP.
    /// </summary>
    private readonly HttpClient _httpClientV4 = new(new SocketsHttpHandler()
    {
        ConnectCallback = static async (context, cancellationToken) =>
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
            PingReply reply = await ping.SendPingAsync(uri.Host, TimeSpan.FromMilliseconds(1000));

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

    public async Task<string> GetWebAsync(Uri url, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true, bool noLogging = false)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);
        AddHeaders(addressFamily, headers, acceptJson, noCache);
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;
        HttpResponseMessage? response = null;

        try
        {
            int retryCount = 0;
            response = await client.GetAsync(url);
            while (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                _logger.BotRatelimited(url, retryCount);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                if (retryCount is not 7)
                    retryCount++;

                response = await client.GetAsync(url);
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (HttpRequestException ex)
        {
            if (!noLogging)
                _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);

            throw;
        }
        catch (Exception ex)
        {
            if (!noLogging)
                _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);

            throw;
        }
        finally
        {
            response?.Dispose();
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

            throw new HttpRequestException(response.ReasonPhrase);
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

    public async Task<string> UploadAsync(Uri url, string file, string fileName, string filePath, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        AddressFamily addressFamily = await GetPreferredIpMethodAsync(url);
        AddHeaders(addressFamily, headers, acceptJson, noCache);
        HttpClient client = (addressFamily is AddressFamily.InterNetworkV6) ? _httpClient : _httpClientV4;

        try
        {
            byte[] fileBytes = await FileOperations.GetBase64BytesFromFileAsync(file);
            string base64String = Convert.ToBase64String(fileBytes);

            using HttpContent jsonPayload = new StringContent(JsonSerializer.Serialize<AzuraFileUploadRecord>(new($"{filePath}/{fileName}", base64String)), Encoding.UTF8, MediaType);
            using HttpResponseMessage response = await client.PostAsync(url, jsonPayload);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();

            _logger.WebRequestFailed(HttpMethod.Post, response.ReasonPhrase ?? string.Empty, url);

            return string.Empty;
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

    private void AddHeaders(AddressFamily addressFamily, Dictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        string botName = SoftwareStats.GetAppName.Replace("Bot", string.Empty, StringComparison.OrdinalIgnoreCase);
        string botVersion = SoftwareStats.GetAppVersion;
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

        if (iPAddresses.Length is 1)
            return iPAddresses[0].AddressFamily;

        // If we have multiple addresses, we need to determine which one to use
        // Prefer IPv6 over IPv4
        foreach (IPAddress _ in iPAddresses.Where(static ip => ip.AddressFamily is AddressFamily.InterNetworkV6))
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
            // Test if the host is reachable within 5 seconds
            using Socket socket = new(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            await socket.ConnectAsync(url.Host, 80, cts.Token);

            return true;
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is SocketException)
        {
            return false;
        }
    }
}
