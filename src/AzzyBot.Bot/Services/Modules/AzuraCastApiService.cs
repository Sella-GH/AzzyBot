using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;

using DSharpPlus.Commands.Processors.SlashCommands;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastApiService(ILogger<AzuraCastApiService> logger, DiscordBotService botService, WebRequestService webService)
{
    private readonly ILogger<AzuraCastApiService> _logger = logger;
    private readonly DiscordBotService _botService = botService;
    private readonly WebRequestService _webService = webService;
    public const string AzuraCastPermissionsWiki = "Please review your [permission](https://github.com/Sella-GH/AzzyBot/wiki/AzuraCast-API-Key-required-permissions) set.";

    public string FilePath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "AzuraCast", "Files");

    private static Dictionary<string, string> CreateHeader(string apiKey)
    {
        return new(1)
        {
            ["X-API-Key"] = apiKey
        };
    }

    public async Task CheckForApiPermissionsAsync(AzuraCastEntity azuraCast)
    {
        ArgumentNullException.ThrowIfNull(azuraCast);

        await CheckForAdminApiPermissionsAsync(azuraCast);
        foreach (AzuraCastStationEntity station in azuraCast.Stations)
        {
            await CheckForStationApiPermissionsAsync(station);
        }
    }

    public Task CheckForApiPermissionsAsync(AzuraCastStationEntity station)
    {
        ArgumentNullException.ThrowIfNull(station);

        return CheckForStationApiPermissionsAsync(station);
    }

    private async Task CheckForAdminApiPermissionsAsync(AzuraCastEntity azuraCast)
    {
        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));
        string apiUrl = $"{baseUrl}/api";
        List<Uri> apis = new(4)
        {
            new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Logs}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Server}/{AzuraApiEndpoints.Stats}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Stations}")
        };

        if (azuraCast.Checks.Updates)
            apis.Add(new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}"));

        IEnumerable<string> missing = await ExecuteApiPermissionCheckAsync(apis, Crypto.Decrypt(azuraCast.AdminApiKey));
        if (!missing.Any())
            return;

        StringBuilder builder = new();
        builder.AppendLine("I can't access the following administrative endpoints:");
        foreach (string api in missing)
        {
            builder.AppendLine(api);
        }

        builder.AppendLine(AzuraCastPermissionsWiki);

        await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, builder.ToString());
    }

    private async Task CheckForStationApiPermissionsAsync(AzuraCastStationEntity station)
    {
        Uri baseUrl = new(Crypto.Decrypt(station.AzuraCast.BaseUrl));
        string apiUrl = $"{baseUrl}/api";
        int stationId = station.StationId;
        AzuraAdminStationConfigRecord? config = await GetStationAdminConfigAsync(baseUrl, Crypto.Decrypt(station.AzuraCast.AdminApiKey), stationId);
        if (config is null)
            return;

        List<Uri> apis = new(7)
        {
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.History}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlists}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Queue}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Status}")
        };

        if (config.EnableRequests)
            apis.Add(new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}"));

        if (station.Checks.FileChanges)
            apis.Add(new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}"));

        string apiKey = (string.IsNullOrEmpty(station.ApiKey)) ? station.AzuraCast.AdminApiKey : station.ApiKey;
        IEnumerable<string> missing = await ExecuteApiPermissionCheckAsync(apis, Crypto.Decrypt(apiKey));
        if (!missing.Any())
            return;

        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"I can't access the following endpoints for station **{config.Name}**:");
        foreach (string api in missing)
        {
            builder.AppendLine(api);
        }

        builder.AppendLine(AzuraCastPermissionsWiki);

        await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, builder.ToString());
    }

    private async Task<IReadOnlyList<string>> ExecuteApiPermissionCheckAsync(List<Uri> apis, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(apis);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(apis.Count);

        IReadOnlyList<bool> checks = await _webService.CheckForApiPermissionsAsync(apis, CreateHeader(apiKey));
        List<string> missing = new(apis.Count);
        for (int i = 0; i < checks.Count; i++)
        {
            if (!checks[i])
                missing.Add(apis[i].AbsolutePath);
        }

        return missing;
    }

    private async Task DeleteToApiAsync(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        Uri uri = new($"{baseUrl}api/{endpoint}");
        try
        {
            await _webService.DeleteAsync(uri, headers);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed {HttpMethod.Delete} to API, url: {uri}", ex);
        }
    }

    private async Task<string?> GetFromApiAsync(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        Uri url = new($"{baseUrl}api/{endpoint}");
        try
        {
            return await _webService.GetWebAsync(url, headers, acceptJson: true);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed {HttpMethod.Get} from API, url: {url}", ex);
        }
    }

    private async Task<T?> GetFromApiAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null, bool noLogging = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        Uri uri = new($"{baseUrl}api/{endpoint}");
        string? body = await _webService.GetWebAsync(uri, headers, acceptJson: true, noLogging: noLogging);
        if (body is null)
            return default;

        if (typeof(T) == typeof(string))
        {
            object? obj = body;
            return (T)obj;
        }

        try
        {
            return (T)JsonSerializer.Deserialize(body, JsonSourceGen.Default.GetTypeInfo(typeof(T))!)!;
        }
        catch (JsonException jsonEx)
        {
            AzuraErrorRecord? error;
            try
            {
                // See if we can catch an error message
                error = JsonSerializer.Deserialize(body, JsonSourceGen.Default.AzuraErrorRecord);
                if (error is not null)
                {
                    await _botService.LogExceptionAsync(new InvalidOperationException($"API returned an error: {error.Message}"), DateTimeOffset.Now);
                    return default;
                }
            }
            catch (JsonException errorEx)
            {
                throw new InvalidOperationException($"Failed to deserialize body: {body}", errorEx);
            }

            throw new InvalidOperationException($"Failed to deserialize body: {body}", jsonEx);
        }
    }

    private async Task<IEnumerable<T>?> GetFromApiListAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        string? body = await GetFromApiAsync(baseUrl, endpoint, headers);
        if (body is null)
            return default;

        try
        {
            return (IEnumerable<T>)JsonSerializer.Deserialize(body, JsonSourceGen.Default.GetTypeInfo(typeof(IEnumerable<T>))!)!;
        }
        catch (JsonException jsonEx)
        {
            AzuraErrorRecord? error;
            try
            {
                // See if we can catch an error message
                error = JsonSerializer.Deserialize(body, JsonSourceGen.Default.AzuraErrorRecord);
                if (error is not null)
                {
                    await _botService.LogExceptionAsync(new InvalidOperationException($"API returned an error: {error.Message}"), DateTimeOffset.Now);
                    return default;
                }
            }
            catch (JsonException errorEx)
            {
                throw new InvalidOperationException($"Failed to deserialize body: {body}", errorEx);
            }

            throw new InvalidOperationException($"Failed to deserialize body: {body}", jsonEx);
        }
    }

    private string GetLocalFile(int guildId, int azuraCastId, int databaseId, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(databaseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        IEnumerable<string> files = FileOperations.GetFilesInDirectory(FilePath);
        string fileName = $"{guildId}-{azuraCastId}-{databaseId}-{stationId}-files.json";

        return files.FirstOrDefault(f => f.Contains(fileName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private async Task PostToApiAsync(Uri baseUrl, string endpoint, string? content = null, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        Uri uri = new($"{baseUrl}api/{endpoint}");
        try
        {
            await _webService.PostWebAsync(uri, content, headers);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed {HttpMethod.Post} to API, url: {uri}", ex);
        }
    }

    private async Task PutToApiAsync(Uri baseUrl, string endpoint, string? content = null, Dictionary<string, string>? headers = null, bool ignoreException = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        Uri uri = new($"{baseUrl}api/{endpoint}");
        try
        {
            await _webService.PutWebAsync(uri, content, headers);
        }
        catch (HttpRequestException ex)
        {
            if (ignoreException)
                return;

            throw new InvalidOperationException($"Failed {HttpMethod.Put} to API, url: {uri}", ex);
        }
    }

    private async Task<string?> UploadToApiAsync(Uri baseUrl, string endpoint, string file, string fileName, string filePath, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Uri uri = new($"{baseUrl}api/{endpoint}");
        try
        {
            return await _webService.UploadAsync(uri, file, fileName, filePath, headers, true);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed {HttpMethod.Post} to API, url: {uri}", ex);
        }
    }

    public async Task DeleteStationSongRequestAsync(Uri baseUrl, string apiKey, int stationId, int requestId = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}/" + ((requestId is 0) ? AzuraApiEndpoints.Clear : requestId);

        if (requestId is 0)
        {
            await PostToApiAsync(baseUrl, endpoint, headers: CreateHeader(apiKey));
        }
        else
        {
            await DeleteToApiAsync(baseUrl, endpoint, CreateHeader(apiKey));
        }
    }

    public async Task DownloadPlaylistAsync(Uri url, string apiKey, string downloadPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadPath);

        await _webService.DownloadAsync(url, downloadPath, CreateHeader(apiKey), acceptJson: true);
    }

    public async Task<IEnumerable<AzuraFilesRecord>> GetFilesLocalAsync(int guildId, int azuraCastId, int databaseId, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(azuraCastId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(databaseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string file = GetLocalFile(guildId, azuraCastId, databaseId, stationId);
        if (string.IsNullOrEmpty(file))
            return [];

        string content = await FileOperations.GetFileContentAsync(file);
        if (string.IsNullOrEmpty(content))
            return [];

        try
        {
            return JsonSerializer.Deserialize(content, JsonSourceGen.Default.IEnumerableAzuraFilesRecord) ?? throw new InvalidOperationException($"Could not deserialize content: {content}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize content: {content}", ex);
        }
    }

    public Task<IEnumerable<T>?> GetFilesOnlineAsync<T>(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}";

        return GetFromApiListAsync<T>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task<AzuraHardwareStatsRecord?> GetHardwareStatsAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        const string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Server}/{AzuraApiEndpoints.Stats}";
        AzuraHardwareStatsRecord? stats = await GetFromApiAsync<AzuraHardwareStatsRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        if (stats is null)
            return null;

        stats.Ping = await _webService.GetPingAsync(baseUrl);

        return stats;
    }

    public Task<AzuraStatusRecord?> GetInstanceStatusAsync(Uri baseUrl)
    {
        const string endpoint = AzuraApiEndpoints.Status;

        return GetFromApiAsync<AzuraStatusRecord>(baseUrl, endpoint, noLogging: true);
    }

    public Task<AzuraNowPlayingDataRecord?> GetNowPlayingAsync(Uri baseUrl, int stationId, bool noLogging = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.NowPlaying}/{stationId}";

        return GetFromApiAsync<AzuraNowPlayingDataRecord>(baseUrl, endpoint, noLogging: noLogging);
    }

    public Task<AzuraPlaylistRecord?> GetPlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlist}/{playlistId}";

        return GetFromApiAsync<AzuraPlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraPlaylistRecord>?> GetPlaylistsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlists}";

        return GetFromApiListAsync<AzuraPlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task<IEnumerable<AzuraPlaylistRecord>?> GetPlaylistsWithRequestsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        IEnumerable<AzuraPlaylistRecord>? playlists = await GetPlaylistsAsync(baseUrl, apiKey, stationId);

        return playlists?.Where(static p => p.IncludeInRequests);
    }

    public async Task<AzuraRequestRecord?> GetRequestableSongAsync(Uri baseUrl, string apiKey, int stationId, string? songId = null, string? name = null, string? artist = null, string? album = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        IEnumerable<AzuraRequestRecord>? songs = await GetRequestableSongsAsync(baseUrl, apiKey, stationId);

        return songs?.FirstOrDefault(s =>
            (songId is null || s.Song.SongId == songId) &&
            (name is null || s.Song.Title == name) &&
            (artist is null || s.Song.Artist == artist) &&
            (album is null || s.Song.Album == album)
            );
    }

    public Task<IEnumerable<AzuraRequestRecord>?> GetRequestableSongsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Requests}";

        return GetFromApiListAsync<AzuraRequestRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraMediaItemRecord>?> GetSongsInPlaylistAsync(Uri baseUrl, string apiKey, int stationId, AzuraPlaylistRecord playlist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentNullException.ThrowIfNull(playlist);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}/{AzuraApiFilters.List}?{AzuraApiFilters.SearchPhrase}={AzuraApiFilters.Playlist}:{playlist.ShortName}";

        return GetFromApiListAsync<AzuraMediaItemRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task<AzuraSongDataRecord?> GetSongInfoAsync(Uri baseUrl, string apiKey, AzuraCastStationEntity station, bool online, string? uniqueId = null, string? songId = null, string? name = null, string? artist = null, string? album = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(station);

        IEnumerable<AzuraFilesRecord>? songs = (online) ? await GetFilesOnlineAsync<AzuraFilesRecord>(baseUrl, apiKey, station.StationId) : await GetFilesLocalAsync(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
        if (songs is null)
            return null;

        AzuraFilesRecord? song = songs.FirstOrDefault(s =>
            (uniqueId is null || s.UniqueId == uniqueId) &&
            (songId is null || s.SongId == songId) &&
            (name is null || s.Title == name) &&
            (artist is null || s.Artist == artist) &&
            (album is null || s.Album == album)
            ) ??
            throw new InvalidOperationException($"Song {name} not found.");

        return new()
        {
            UniqueId = song.UniqueId,
            SongId = song.SongId,
            Album = song.Album,
            Artist = song.Artist,
            Title = song.Title,
            Text = $"{song.Title} - {song.Artist}",
            Art = $"{baseUrl}api/{AzuraApiEndpoints.Station}/{station.StationId}/{AzuraApiEndpoints.Art}/{song.UniqueId}"
        };
    }

    public Task<AzuraStationRecord?> GetStationAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}";

        return GetFromApiAsync<AzuraStationRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<AzuraAdminStationConfigRecord?> GetStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        return GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraStationHistoryItemRecord>?> GetStationHistoryAsync(Uri baseUrl, string apiKey, int stationId, in DateTimeOffset start, in DateTimeOffset end)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.History}?{AzuraApiFilters.Start}={start:yyyy-MM-dd}&{AzuraApiFilters.End}={end:yyyy-MM-dd}";

        return GetFromApiListAsync<AzuraStationHistoryItemRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraStationListenerRecord>?> GetStationListenersAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Listeners}";

        return GetFromApiListAsync<AzuraStationListenerRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraHlsMountRecord>?> GetStationHlsMountPointsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.HlsStreams}";

        return GetFromApiListAsync<AzuraHlsMountRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraStationQueueItemDetailedRecord>?> GetStationQueueAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Queue}";

        return GetFromApiListAsync<AzuraStationQueueItemDetailedRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraRequestQueueItemRecord>?> GetStationRequestItemsAsync(Uri baseUrl, string apiKey, int stationId, bool history)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = (history)
            ? $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}?{AzuraApiFilters.Type}={AzuraApiFilters.History}"
            : $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}?{AzuraApiFilters.Type}={AzuraApiFilters.Pending}";

        return GetFromApiListAsync<AzuraRequestQueueItemRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task<AzuraSystemLogRecord?> GetSystemLogAsync(Uri baseUrl, string apiKey, string logName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(logName);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Log}/{logName}";

        try
        {
            return await GetFromApiAsync<AzuraSystemLogRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public Task<AzuraSystemLogsRecord?> GetSystemLogsAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Logs}";

        return GetFromApiAsync<AzuraSystemLogsRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<string?> GetUpdatesAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}";

        return GetFromApiAsync<string>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task ModifyStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId, AzuraAdminStationConfigRecord config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentNullException.ThrowIfNull(config);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        await PutToApiAsync(baseUrl, endpoint, JsonSerializer.Serialize(config, JsonSourceGen.Default.AzuraAdminStationConfigRecord), CreateHeader(apiKey));
    }

    public async Task RequestInternalSongAsync(Uri baseUrl, string apiKey, int stationId, string songPath)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(songPath);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}/{AzuraApiEndpoints.Batch}";

        // Get the last slash to separate the path from the song
        int lastSlash = songPath.LastIndexOf('/');
        if (lastSlash is -1)
            throw new InvalidOperationException($"Invalid song path: {songPath}");

        AzuraInternalRequestRecord songRequest = new(songPath[..lastSlash], AzuraApiEndpoints.Queue, [songPath]);

        await PutToApiAsync(baseUrl, endpoint, JsonSerializer.Serialize(songRequest, JsonSourceGen.Default.AzuraInternalRequestRecord), CreateHeader(apiKey));
    }

    public async Task RequestSongAsync(Uri baseUrl, int stationId, string requestId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Request}/{requestId}";

        await PostToApiAsync(baseUrl, endpoint);
    }

    public async Task SkipSongAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Backend}/{AzuraApiEndpoints.Skip}";

        await PostToApiAsync(baseUrl, endpoint, headers: CreateHeader(apiKey));
    }

    public async Task<bool> StartStationAsync(Uri baseUrl, string apiKey, int stationId, SlashCommandContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentNullException.ThrowIfNull(context);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";
        AzuraAdminStationConfigRecord? config = await GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        if (config is null)
            return false;

        config.IsEnabled = true;
        await ModifyStationAdminConfigAsync(baseUrl, apiKey, stationId, config);

        await context.EditResponseAsync("I activated the station, please wait for it to start up.");
        await Task.Delay(TimeSpan.FromSeconds(10));

        endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Status}";
        AzuraStationStatusRecord? status = await GetFromApiAsync<AzuraStationStatusRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        if (status is null)
            return false;

        if (!status.BackendRunning || !status.FrontendRunning)
            throw new InvalidOperationException("Station failed to start.");

        if (!config.BackendConfig.WritePlaylistsToLiquidsoap)
            return true;

        // Delay to ensure the station is fully started
        await Task.Delay(TimeSpan.FromSeconds(5));

        AzuraNowPlayingDataRecord? nowPlaying = null;
        bool? firstTime = null;
        while (nowPlaying is null)
        {
            try
            {
                nowPlaying = await GetNowPlayingAsync(baseUrl, stationId, true);
            }
            catch (Exception e) when (e is HttpRequestException or InvalidOperationException or JsonException)
            {
                if (!firstTime.HasValue)
                    firstTime = true;

                if (firstTime.Value)
                {
                    await context.EditResponseAsync("You have activated the option \"**Always Write Playlists to Liquidsoap**\" which means you have to wait more time until you can finally use your station.\nI inform you when it's finished.");
                    firstTime = false;
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        await context.EditResponseAsync("All playlists have been written back to liquidsoap.");

        return true;
    }

    public async Task<bool> StopStationAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        AzuraAdminStationConfigRecord? config = await GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        if (config is null)
            return false;

        config.IsEnabled = false;

        await ModifyStationAdminConfigAsync(baseUrl, apiKey, stationId, config);

        return true;
    }

    public async Task<List<AzuraPlaylistStateRecord>?> SwitchPlaylistsAsync(Uri baseUrl, string apiKey, int stationId, int playlistId, bool removeOld)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId);

        IEnumerable<AzuraPlaylistRecord>? playlists = await GetPlaylistsAsync(baseUrl, apiKey, stationId);
        if (playlists is null)
            return null;

        List<AzuraPlaylistStateRecord> states = new(1);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlist}/{playlistId}/{AzuraApiEndpoints.Toggle}";

        await PutToApiAsync(baseUrl, endpoint, headers: CreateHeader(apiKey));
    }

    public async Task UpdateInstanceAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}";

        try
        {
            await PutToApiAsync(baseUrl, endpoint, headers: CreateHeader(apiKey), ignoreException: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            _logger.WebRequestExpectedFailure(HttpMethod.Put, baseUrl, ex.Message);
        }

        bool online = false;
        AzuraStatusRecord? status;
        while (!online)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));

            try
            {
                status = await GetInstanceStatusAsync(baseUrl);
                if (status is null)
                    continue;

                online = status.Online;
            }
            catch (HttpRequestException)
            {
                online = false;
            }
        }
    }

    public async Task<T?> UploadFileAsync<T>(Uri baseUrl, string apiKey, int stationId, string file, string fileName, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}";
        string? result = await UploadToApiAsync(baseUrl, endpoint, file, fileName, filePath, CreateHeader(apiKey));
        if (result is null)
            return default;

        try
        {
            return (T)JsonSerializer.Deserialize(result, JsonSourceGen.Default.GetTypeInfo(typeof(T))!)!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize result: {result}", ex);
        }
    }
}
