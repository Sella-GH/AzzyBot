using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
#if DEBUG || DOCKER_DEBUG
using System.Net.Http.Headers;
#endif
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Net.Quic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Models;
using AzzyBot.Bot.Resources;
using AzzyBot.Bot.Services.Interfaces;
#if DEBUG || DOCKER_DEBUG
using AzzyBot.Bot.Structs;
#endif
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Utilities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class WebRequestService : IWebRequestService
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<WebRequestService> _logger;
    private const string MediaTypeJson = MediaTypeNames.Application.Json;
    private static readonly string HttpClientName = SoftwareStats.GetAppName;

    public WebRequestService(IHttpClientFactory factory, ILogger<WebRequestService> logger)
    {
        _factory = factory;
        _logger = logger;

        _logger.Http3QuicSupport(QuicConnection.IsSupported);
    }

    public async Task<IReadOnlyList<bool>> CheckForApiPermissionsAsync(IReadOnlyList<Uri> urls, IReadOnlyDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(urls);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(urls.Count);

        using HttpClient client = _factory.CreateClient(HttpClientName);

        List<bool> results = new(urls.Count);
        foreach (Uri url in urls)
        {
            bool success = false;

            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, url);
                PrepareRequests(request, headers, acceptJson: true, noCache: true);

                using HttpResponseMessage? response = await client.SendAsync(request);
                success = response.IsSuccessStatusCode;
            }
            catch (InvalidOperationException)
            {
                _logger.WebInvalidUri(url);
                success = false;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);
                success = false;
            }

            results.Add(success);
        }

        return results;
    }

    public async Task DeleteAsync(Uri uri, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        using HttpClient client = _factory.CreateClient(HttpClientName);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Delete, uri);
            PrepareRequests(request, headers, acceptJson: acceptJson, noCache: noCache);

            using HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return;

            _logger.WebRequestFailed(HttpMethod.Delete, response.ReasonPhrase ?? string.Empty, uri);
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(uri);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Delete, ex.Message, uri);
            throw;
        }
    }

    public async Task<string> DownloadAsync(Uri url, string downloadPath, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool acceptImage = false, bool noCache = true)
    {
        try
        {
            using HttpClient client = _factory.CreateClient(HttpClientName);
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            PrepareRequests(request, headers, acceptJson: acceptJson, acceptImage: acceptImage, noCache: noCache);

            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (acceptImage)
            {
                downloadPath += contentType switch
                {
                    "image/jpeg" or "image/jpg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/bmp" => ".bmp",
                    "image/tiff" => ".tiff",
                    "image/webp" => ".webp",
                    _ => string.Empty
                };
            }

            await using Stream contentStream = await response.Content.ReadAsStreamAsync();
            await using FileStream fileStream = new(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await contentStream.CopyToAsync(fileStream);

            return downloadPath;
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);
            throw;
        }
    }

    public async Task<AzzyIpAddressModel> GetIpAddressesAsync()
    {
        string ipv4 = string.Empty;
        string ipv6 = string.Empty;

        try
        {
            using HttpClient client = _factory.CreateClient(HttpClientName);

            ipv4 = await client.GetStringAsync(new Uri(UriStrings.GetIpv4Uri));
            ipv6 = await client.GetStringAsync(new Uri(UriStrings.GetIpv6Uri));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            if (string.IsNullOrEmpty(ipv4))
                _logger.WebRequestFailed(HttpMethod.Get, "Failed to get IPv4 address", new(UriStrings.GetIpv4Uri));

            if (string.IsNullOrEmpty(ipv6))
                _logger.WebRequestFailed(HttpMethod.Get, "Failed to get IPv6 address", new(UriStrings.GetIpv6Uri));
        }

        return new(ipv4, ipv6);
    }

    public async Task<long> GetPingAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

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
        catch (Exception ex) when (ex is PingException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, uri);
            throw;
        }
    }

    public async Task<string?> GetWebAsync(Uri url, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true, bool noLogging = false)
    {
        try
        {
            using HttpClient client = _factory.CreateClient(HttpClientName);
            HttpStatusCode status;
            string? responseContent;
            using (HttpRequestMessage request = new(HttpMethod.Get, url))
            {
                PrepareRequests(request, headers, acceptJson: acceptJson, noCache: noCache);
                using HttpResponseMessage response = await client.SendAsync(request);
                status = response.StatusCode;
                responseContent = await response.Content.ReadAsStringAsync();
            }

            const int maxRetries = 7;
            int retryCount = 0;
            while (status is HttpStatusCode.TooManyRequests && retryCount is < maxRetries)
            {
                _logger.BotRatelimited(url, retryCount);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                retryCount++;

                using HttpRequestMessage retryRequest = new(HttpMethod.Get, url);
                PrepareRequests(retryRequest, headers, acceptJson: acceptJson, noCache: noCache);

                using HttpResponseMessage response = await client.SendAsync(retryRequest);
                status = response.StatusCode;
                responseContent = await response.Content.ReadAsStringAsync();
            }

            int statusCode = (int)status;
            return statusCode switch
            {
                >= 100 and <= 199 => null,
                >= 200 and <= 399 => responseContent,
                >= 400 and <= 499 => null,
                >= 500 => throw new HttpRequestException($"Server returned an error: {statusCode}.", null, status),
                _ => throw new HttpRequestException($"Unexpected status code: {statusCode}.", null, status)
            };
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
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
    }

#if DEBUG || DOCKER_DEBUG
    public async Task<AzzyDebugWebRequestStruct> DebugGetWebAsync(Uri url)
    {
        try
        {
            using HttpClient client = _factory.CreateClient(HttpClientName);
            HttpStatusCode status;
            Version httpVersion;
            HttpRequestHeaders reqHeaders;
            HttpResponseHeaders resHeaders;
            string? responseContent;
            using (HttpRequestMessage request = new(HttpMethod.Get, url))
            {
                PrepareRequests(request, headers: null, acceptJson: true, noCache: true);
                using HttpResponseMessage response = await client.SendAsync(request);
                httpVersion = response.Version;
                status = response.StatusCode;
                reqHeaders = request.Headers;
                resHeaders = response.Headers;
                responseContent = await response.Content.ReadAsStringAsync();
            }

            const int maxRetries = 7;
            int retryCount = 0;
            while (status is HttpStatusCode.TooManyRequests && retryCount is < maxRetries)
            {
                _logger.BotRatelimited(url, retryCount);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                retryCount++;

                using HttpRequestMessage retryRequest = new(HttpMethod.Get, url);
                PrepareRequests(retryRequest, headers: null, acceptJson: true, noCache: true);

                using HttpResponseMessage response = await client.SendAsync(retryRequest);
                httpVersion = response.Version;
                status = response.StatusCode;
                reqHeaders = retryRequest.Headers;
                resHeaders = response.Headers;
                responseContent = await response.Content.ReadAsStringAsync();
            }

            return new()
            {
                RequestUri = url,
                Method = HttpMethod.Get,
                HttpVersion = httpVersion,
                StatusCode = status,
                ReqHeaders = reqHeaders,
                ResHeaders = resHeaders,
                Retries = retryCount,
                Content = responseContent
            };
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);
            throw;
        }
        catch (Exception ex)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, url);
            throw;
        }
    }
#endif

    public async Task PostWebAsync(Uri url, string? content = null, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        try
        {
            using HttpClient client = _factory.CreateClient(HttpClientName);
            using HttpContent httpContent = new StringContent(content ?? string.Empty, Encoding.UTF8, MediaTypeJson);
            using HttpRequestMessage request = new(HttpMethod.Post, url)
            {
                Content = httpContent
            };

            PrepareRequests(request, headers, acceptJson: acceptJson, noCache: noCache);

            using HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return;

            throw new HttpRequestException(response.ReasonPhrase);
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Post, ex.Message, url);
            throw;
        }
    }

    public async Task PutWebAsync(Uri url, string? content = null, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        try
        {
            using HttpClient client = _factory.CreateClient(HttpClientName);
            using HttpContent httpContent = new StringContent(content ?? string.Empty, Encoding.UTF8, MediaTypeJson);
            using HttpRequestMessage request = new(HttpMethod.Put, url)
            {
                Content = httpContent
            };

            PrepareRequests(request, headers, acceptJson: acceptJson, noCache: noCache);

            using HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return;

            _logger.WebRequestFailed(HttpMethod.Put, response.ReasonPhrase ?? string.Empty, url);
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Put, ex.Message, url);
            throw;
        }
    }

    public async Task<string?> UploadAsync(Uri url, string file, string fileName, string filePath, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        try
        {
            byte[] fileBytes = await FileOperations.GetBase64BytesFromFileAsync(file);
            string base64String = Convert.ToBase64String(fileBytes);
            string json = JsonSerializer.Serialize(new($"{filePath}/{fileName}", base64String), JsonSourceGen.Default.AzuraFileUploadModel);

            using HttpClient client = _factory.CreateClient(HttpClientName);
            using HttpContent httpContent = new StringContent(json, Encoding.UTF8, MediaTypeJson);
            using HttpRequestMessage request = new(HttpMethod.Post, url)
            {
                Content = httpContent
            };

            PrepareRequests(request, headers, acceptJson: acceptJson, noCache: noCache);

            using HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();

            _logger.WebRequestFailed(HttpMethod.Post, response.ReasonPhrase ?? string.Empty, url);

            return null;
        }
        catch (InvalidOperationException)
        {
            _logger.WebInvalidUri(url);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Post, ex.Message, url);
            throw;
        }
    }

    private static void PrepareRequests(HttpRequestMessage request, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool acceptImage = false, bool noCache = true)
    {
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        if (acceptImage && acceptJson)
            throw new ArgumentException("Cannot accept both image and JSON content types.");

        if (acceptImage)
            request.Headers.Accept.Add(new("image/*")); // Manual to allow all image types

        if (acceptJson)
            request.Headers.Accept.Add(new(MediaTypeJson));

        if (noCache)
        {
            request.Headers.CacheControl = new() { NoCache = true };
            request.Headers.Pragma.Add(new("no-cache"));
        }

        if (headers is null)
            return;

        foreach (KeyValuePair<string, string> header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
    }
}
