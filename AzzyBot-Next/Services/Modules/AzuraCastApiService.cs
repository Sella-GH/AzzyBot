using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Helpers;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Commands;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastApiService(WebRequestService webService)
{
    private readonly WebRequestService _webService = webService;

    public string FilePath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "AzuraCast", "Files");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private static Dictionary<string, string> CreateHeader(string apiKey)
    {
        return new()
        {
            ["X-API-Key"] = apiKey
        };
    }

    private async Task<string> GetFromApiAsync(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri url = new($"{baseUrl}api/{endpoint}");
        string body = await _webService.GetWebAsync(url, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"API response is empty, url: {url}");

        return body;
    }

    private async Task<T> GetFromApiAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        string body = await _webService.GetWebAsync(uri, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"API response is empty, url: {uri}");

        return JsonSerializer.Deserialize<T>(body) ?? throw new InvalidOperationException($"Could not deserialize body: {body}");
    }

    private async Task<IReadOnlyList<T>> GetFromApiListAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        string body = await GetFromApiAsync(baseUrl, endpoint, headers);

        return JsonSerializer.Deserialize<List<T>>(body) ?? throw new InvalidOperationException($"Could not deserialize body: {body}");
    }

    private async Task PostToApiAsync(Uri baseUrl, string endpoint, string? content = null, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        bool success = await _webService.PostWebAsync(uri, content, headers);
        if (!success)
            throw new InvalidOperationException($"Failed POST to API, url: {uri}");
    }

    private async Task PutToApiAsync(Uri baseUrl, string endpoint, string? content = null, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        bool success = await _webService.PutWebAsync(uri, content, headers);
        if (!success)
            throw new InvalidOperationException($"Failed PUT to API, url: {uri}");
    }

    public async Task DownloadPlaylistAsync(Uri url, string apiKey, string downloadPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadPath, nameof(downloadPath));

        await _webService.DownloadAsync(url, downloadPath, CreateHeader(apiKey));
    }

    public async Task<IReadOnlyList<AzuraFilesRecord>> GetFilesLocalAsync(int databaseId, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        IReadOnlyList<string> files = FileOperations.GetFilesInDirectory(FilePath);
        List<AzuraFilesRecord> records = [];

        string file = files.FirstOrDefault(f => f.Contains($"{databaseId}-{stationId}-files.json", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(file))
            return records;

        string content = await FileOperations.GetFileContentAsync(file);
        if (string.IsNullOrWhiteSpace(content))
            return records;

        return JsonSerializer.Deserialize<List<AzuraFilesRecord>>(content) ?? throw new InvalidOperationException($"Could not deserialize content: {content}");
    }

    public Task<IReadOnlyList<AzuraFilesRecord>> GetFilesOnlineAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Files}";

        return GetFromApiListAsync<AzuraFilesRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<AzuraHardwareStatsRecord> GetHardwareStatsAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        const string endpoint = $"{ApiEndpoints.Admin}/{ApiEndpoints.Server}/{ApiEndpoints.Stats}";

        return GetFromApiAsync<AzuraHardwareStatsRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<AzuraNowPlayingDataRecord> GetNowPlayingAsync(Uri baseUrl, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.NowPlaying}/{stationId}";

        return GetFromApiAsync<AzuraNowPlayingDataRecord>(baseUrl, endpoint);
    }

    public Task<AzuraPlaylistRecord> GetPlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Playlist}/{playlistId}";

        return GetFromApiAsync<AzuraPlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IReadOnlyList<AzuraPlaylistRecord>> GetPlaylistsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Playlists}";

        return GetFromApiListAsync<AzuraPlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<AzuraAdminStationConfigRecord> GetStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Admin}/{ApiEndpoints.Station}/{stationId}";

        return GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<AzuraUpdateRecord> GetUpdatesAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        string endpoint = $"{ApiEndpoints.Admin}/{ApiEndpoints.Updates}";

        return GetFromApiAsync<AzuraUpdateRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task SkipSongAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Backend}/{ApiEndpoints.Skip}";

        await PostToApiAsync(baseUrl, endpoint, null, CreateHeader(apiKey));
    }

    public async Task StartStationAsync(Uri baseUrl, string apiKey, int stationId, CommandContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        string endpoint = $"{ApiEndpoints.Admin}/{ApiEndpoints.Station}/{stationId}";
        AzuraAdminStationConfigRecord config = await GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        config.IsEnabled = true;
        await PutToApiAsync(baseUrl, endpoint, JsonSerializer.Serialize(config, _jsonOptions), CreateHeader(apiKey));

        await context.EditResponseAsync("Step 1: I activated the station, please wait for setup.");
        await Task.Delay(TimeSpan.FromSeconds(3));

        endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Restart}";
        await PostToApiAsync(baseUrl, endpoint, null, CreateHeader(apiKey));

        await context.EditResponseAsync("Step 2: I started the station, just wait a little more time.");
        await Task.Delay(TimeSpan.FromSeconds(10));

        endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Status}";
        AzuraStationStatusRecord status = await GetFromApiAsync<AzuraStationStatusRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        if (!status.BackendRunning || !status.FrontendRunning)
            throw new InvalidOperationException("Station failed to start.");
    }

    public async Task StopStationAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{ApiEndpoints.Admin}/{ApiEndpoints.Station}/{stationId}";

        AzuraAdminStationConfigRecord config = await GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        config.IsEnabled = false;

        await PutToApiAsync(baseUrl, endpoint, JsonSerializer.Serialize(config, _jsonOptions), CreateHeader(apiKey));
    }

    public async Task<List<AzuraPlaylistStateRecord>> SwitchPlaylistsAsync(Uri baseUrl, string apiKey, int stationId, int playlistId, bool removeOld)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId, nameof(playlistId));

        IReadOnlyList<AzuraPlaylistRecord> playlists = await GetPlaylistsAsync(baseUrl, apiKey, stationId);
        List<AzuraPlaylistStateRecord> states = [];

        if (removeOld)
        {
            foreach (AzuraPlaylistRecord playlist in playlists.Where(p => p.IsEnabled && p.Id != playlistId))
            {
                await TogglePlaylistAsync(baseUrl, apiKey, stationId, playlist.Id);
                states.Add(new(playlist.Name, !playlist.IsEnabled));
            }
        }

        AzuraPlaylistRecord current = playlists.FirstOrDefault(p => p.Id == playlistId) ?? throw new InvalidOperationException($"Playlist with id {playlistId} not found.");
        await TogglePlaylistAsync(baseUrl, apiKey, stationId, current.Id);
        states.Add(new(current.Name, !current.IsEnabled));

        return states;
    }

    public async Task TogglePlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId, nameof(playlistId));

        string endpoint = $"{ApiEndpoints.Station}/{stationId}/{ApiEndpoints.Playlist}/{playlistId}/{ApiEndpoints.Toggle}";

        await PutToApiAsync(baseUrl, endpoint, null, CreateHeader(apiKey));
    }
}
