using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Utilities.Records.AzuraCast;

namespace AzzyBot.Services;

public sealed class AzuraCastService(WebRequestService webService)
{
    private readonly WebRequestService _webService = webService;
    //private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private async Task<string> FetchFromApiAsync(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headers.Count, nameof(headers.Count));

        Uri url = new($"{baseUrl}/api/{endpoint}");
        string body = await _webService.GetWebAsync(url, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"API response is empty, url: {url}");

        return body;
    }

    private async Task<T> FetchFromApiAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headers.Count, nameof(headers.Count));

        Uri uri = new($"{baseUrl}/api/{endpoint}");
        string body = await _webService.GetWebAsync(uri, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"API response is empty, url: {uri}");

        return JsonSerializer.Deserialize<T>(body) ?? throw new InvalidOperationException($"Could not deserialize body: {body}");
    }

    private async Task<IReadOnlyList<T>> FetchFromApiListAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(headers.Count, nameof(headers.Count));

        string body = await FetchFromApiAsync(baseUrl, endpoint, headers);

        return JsonSerializer.Deserialize<List<T>>(body) ?? throw new InvalidOperationException($"Could not deserialize body: {body}");
    }

    public Task<PlaylistRecord> GetPlaylistAsync(Uri baseUrl, int stationId, int playlistId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"station/{stationId}/playlist/{playlistId}";

        return FetchFromApiAsync<PlaylistRecord>(baseUrl, endpoint);
    }

    public Task<IReadOnlyList<PlaylistRecord>> GetPlaylistsAsync(Uri baseUrl, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"station/{stationId}/playlists";

        return FetchFromApiListAsync<PlaylistRecord>(baseUrl, endpoint);
    }

    public Task<NowPlayingDataRecord> GetNowPlayingAsync(Uri baseUrl, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"nowplaying/{stationId}";

        return FetchFromApiAsync<NowPlayingDataRecord>(baseUrl, endpoint);
    }
}
