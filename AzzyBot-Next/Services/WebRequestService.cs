using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using AzzyBot.Logging;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal sealed class WebRequestService(ILogger<WebRequestService> logger) : IDisposable
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Forcing this client to use IPv4, only TCP ports because AzuraCast prefers TCP
    /// </summary>
    internal readonly HttpClient _httpClientV4 = new(new SocketsHttpHandler()
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
    /// Default HttpClient which prefers IPv6
    /// </summary>
    internal readonly HttpClient _httpClient = new()
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

    internal async Task<string> GetWebAsync(Uri url, Dictionary<string, string>? headers = null, bool ipv6 = true)
    {
        if (headers is not null)
            AddHeaders(headers, ipv6);

        HttpClient client = (ipv6) ? _httpClient : _httpClientV4;
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

    private void AddHeaders(Dictionary<string, string> headers, bool ipv6 = true)
    {
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));

        HttpClient client = (ipv6) ? _httpClient : _httpClientV4;
        client.DefaultRequestHeaders.Clear();

        foreach (KeyValuePair<string, string> header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }
}
