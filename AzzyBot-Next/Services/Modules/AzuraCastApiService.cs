using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Helpers;
using AzzyBot.Utilities.Records.AzuraCast;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastApiService(WebRequestService webService)
{
    private readonly WebRequestService _webService = webService;

    public string FilePath { get; } = Path.Combine(Environment.CurrentDirectory, "Modules", "AzuraCast", "Files");
    //private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private async Task<string> FetchFromApiAsync(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri url = new($"{baseUrl}api/{endpoint}");
        string body = await _webService.GetWebAsync(url, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"API response is empty, url: {url}");

        return body;
    }

    private static Dictionary<string, string> CreateHeader(string apiKey)
    {
        return new()
        {
            ["X-API-Key"] = apiKey
        };
    }

    private async Task<T> FetchFromApiAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        string body = await _webService.GetWebAsync(uri, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"API response is empty, url: {uri}");

        return JsonSerializer.Deserialize<T>(body) ?? throw new InvalidOperationException($"Could not deserialize body: {body}");
    }

    private async Task<IReadOnlyList<T>> FetchFromApiListAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        string body = await FetchFromApiAsync(baseUrl, endpoint, headers);

        return JsonSerializer.Deserialize<List<T>>(body) ?? throw new InvalidOperationException($"Could not deserialize body: {body}");
    }

    public async Task<IReadOnlyList<FilesRecord>> GetFilesLocalAsync(int databaseId, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        IReadOnlyList<string> files = FileOperations.GetFilesInDirectory(FilePath);
        List<FilesRecord> records = [];

        string file = files.FirstOrDefault(f => f.Contains($"{databaseId}-{stationId}-files.json", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(file))
            return records;

        string content = await FileOperations.GetFileContentAsync(file);
        if (string.IsNullOrWhiteSpace(content))
            return records;

        return JsonSerializer.Deserialize<List<FilesRecord>>(content) ?? throw new InvalidOperationException($"Could not deserialize content: {content}");
    }

    public Task<IReadOnlyList<FilesRecord>> GetFilesOnlineAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Files}";

        return FetchFromApiListAsync<FilesRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<HardwareStatsRecord> GetHardwareStatsAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        const string endpoint = $"{ApiEndpoints.Admin}/{ApiEndpoints.Server}/{ApiEndpoints.Stats}";

        return FetchFromApiAsync<HardwareStatsRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<NowPlayingDataRecord> GetNowPlayingAsync(Uri baseUrl, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.NowPlaying}/{stationId}";

        return FetchFromApiAsync<NowPlayingDataRecord>(baseUrl, endpoint);
    }

    public Task<PlaylistRecord> GetPlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Playlist}/{playlistId}";

        return FetchFromApiAsync<PlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IReadOnlyList<PlaylistRecord>> GetPlaylistsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Playlists}";

        return FetchFromApiListAsync<PlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<UpdateRecord> GetUpdatesAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        string endpoint = $"{ApiEndpoints.Admin}/{ApiEndpoints.Updates}";

        return FetchFromApiAsync<UpdateRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }
}
