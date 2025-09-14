using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Resources;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class WebRequestService(IHttpClientFactory factory, ILogger<WebRequestService> logger)
{
    private readonly IHttpClientFactory _factory = factory;
    private readonly ILogger _logger = logger;
    private const string MediaType = MediaTypeNames.Application.Json;

    public async Task<IReadOnlyList<bool>> CheckForApiPermissionsAsync(IReadOnlyList<Uri> urls, IReadOnlyDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(urls);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(urls.Count);

        List<bool> results = new(urls.Count);
        foreach (Uri url in urls)
        {
            bool success = false;

            try
            {
                using HttpClient client = _factory.CreateClient();
                AddHeaders(client, headers, true, true);

                using HttpResponseMessage? response = await client.GetAsync(url);
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
        try
        {
            using HttpClient client = _factory.CreateClient();
            AddHeaders(client, headers, acceptJson, noCache);

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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Delete, ex.Message, uri);
            throw;
        }
    }

    public async Task DownloadAsync(Uri url, string downloadPath, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        try
        {
            using HttpClient client = _factory.CreateClient();
            AddHeaders(client, headers, acceptJson, noCache);

            using HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using Stream contentStream = await response.Content.ReadAsStreamAsync();
            await using FileStream fileStream = new(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await contentStream.CopyToAsync(fileStream);
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

    public async Task<AzzyIpAddressRecord> GetIpAddressesAsync()
    {
        string ipv4 = string.Empty;
        string ipv6 = string.Empty;

        try
        {
            using HttpClient client = _factory.CreateClient();

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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.WebRequestFailed(HttpMethod.Get, ex.Message, uri);
            throw;
        }
    }

    public async Task<string?> GetWebAsync(Uri url, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true, bool noLogging = false)
    {
        HttpResponseMessage? response = null;

        try
        {
            using HttpClient client = _factory.CreateClient();
            AddHeaders(client, headers, acceptJson, noCache);

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

            return (response.StatusCode is not HttpStatusCode.Forbidden) ? await response.Content.ReadAsStringAsync() : null;
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
        finally
        {
            response?.Dispose();
        }
    }

    public async Task PostWebAsync(Uri url, string? content = null, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        try
        {
            using HttpClient client = _factory.CreateClient();
            AddHeaders(client, headers, acceptJson, noCache);

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
            using HttpClient client = _factory.CreateClient();
            AddHeaders(client, headers, acceptJson, noCache);

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
            using HttpClient client = _factory.CreateClient();
            AddHeaders(client, headers, acceptJson, noCache);

            byte[] fileBytes = await FileOperations.GetBase64BytesFromFileAsync(file);
            string base64String = Convert.ToBase64String(fileBytes);
            string json = JsonSerializer.Serialize(new($"{filePath}/{fileName}", base64String), JsonSerializationSourceGen.Default.AzuraFileUploadRecord);

            using HttpContent jsonPayload = new StringContent(json, Encoding.UTF8, MediaType);
            using HttpResponseMessage response = await client.PostAsync(url, jsonPayload);
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

    private static void AddHeaders(HttpClient client, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true)
    {
        if (acceptJson)
            client.DefaultRequestHeaders.Accept.Add(new(MediaType));

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
}
