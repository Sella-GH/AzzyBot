using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

using AzzyBot.Bot.Helpers;
using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;

using DSharpPlus.Commands.Processors.SlashCommands;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastApiService(ILogger<AzuraCastApiService> logger, IDiscordBotService botService, IWebRequestService webService) : IAzuraCastApiService
{
    private readonly ILogger<AzuraCastApiService> _logger = logger;
    private readonly IDiscordBotService _botService = botService;
    private readonly IWebRequestService _webService = webService;

    public string AzuraCastPermissionsWiki { get; } = "Please review your [permissions](https://github.com/Sella-GH/AzzyBot/wiki/AzuraCast-API-Key-required-permissions) set.";
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
        string apiUrl = $"{baseUrl}api"; // Omit the trailing slash because it's an Uri
        string adminApiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        List<Uri> apis = new(4)
        {
            new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Logs}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Server}/{AzuraApiEndpoints.Stats}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Stations}")
        };

        if (azuraCast.Checks.Updates)
            apis.Add(new($"{apiUrl}/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}"));

        IEnumerable<string> missing = await ExecuteApiPermissionCheckAsync(apis, adminApiKey);
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
        string apiUrl = $"{baseUrl}api"; // Omit the trailing slash because it's an Uri
        int stationId = station.StationId;
        string adminApiKey = Crypto.Decrypt(station.AzuraCast.AdminApiKey);
        AzuraAdminStationConfigModel? config = await GetStationAdminConfigAsync(baseUrl, adminApiKey, stationId);
        if (config is null)
            return;

        List<Uri> apis = new(6)
        {
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.History}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlists}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Queue}"),
            new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Status}")
        };

        if (config.EnableRequests)
            apis.Add(new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}"));

        if (station.Checks.FileChanges)
            apis.Add(new($"{apiUrl}/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}"));

        string apiKey = (!string.IsNullOrEmpty(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : adminApiKey;
        IEnumerable<string> missing = await ExecuteApiPermissionCheckAsync(apis, apiKey);
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

    private async Task<T?> GetFromApiAsync<T>(Uri baseUrl, string endpoint, JsonTypeInfo<T> jsonType, Dictionary<string, string>? headers = null, bool noLogging = false)
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
            return JsonSerializer.Deserialize(body, jsonType);
        }
        catch (JsonException jsonEx)
        {
            AzuraErrorModel? error;
            try
            {
                // See if we can catch an error message
                error = JsonSerializer.Deserialize(body, JsonSourceGen.Default.AzuraErrorModel);
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

    private async Task<IEnumerable<T>?> GetFromApiListAsync<T>(Uri baseUrl, string endpoint, JsonTypeInfo<IEnumerable<T>> jsonType, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        string? body = await GetFromApiAsync(baseUrl, endpoint, headers);
        if (body is null)
            return default;

        try
        {
            return JsonSerializer.Deserialize(body, jsonType);
        }
        catch (JsonException jsonEx)
        {
            AzuraErrorModel? error;
            try
            {
                // See if we can catch an error message
                error = JsonSerializer.Deserialize(body, JsonSourceGen.Default.AzuraErrorModel);
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

    public Task<string> DownloadSongArtworkAsync(Uri url, string apiKey, string downloadPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadPath);

        return _webService.DownloadAsync(url, downloadPath, CreateHeader(apiKey), acceptImage: true);
    }

    public async Task<IEnumerable<AzuraFilesModel>> GetFilesLocalAsync(int guildId, int azuraCastId, int databaseId, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(azuraCastId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(databaseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string file = GetLocalFile(guildId, azuraCastId, databaseId, stationId);
        if (string.IsNullOrEmpty(file))
        {
            _logger.LocalFileNotFound(stationId, databaseId, azuraCastId, guildId);
            return [];
        }

        string content = await FileOperations.GetFileContentAsync(file);
        if (string.IsNullOrEmpty(content))
        {
            _logger.LocalFileContentNotFound(stationId, databaseId, azuraCastId, guildId);
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize(content, JsonSourceGen.Default.IEnumerableAzuraFilesModel)
                ?? throw new InvalidOperationException($"Could not deserialize content: {content}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize content: {content}", ex);
        }
    }

    public Task<IEnumerable<AzuraFilesModel>?> GetFilesOnlineBasicAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraFilesModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraFilesDetailedModel>?> GetFilesOnlineDetailedAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraFilesDetailedModel, CreateHeader(apiKey));
    }

    public async Task<AzuraHardwareStatsModel?> GetHardwareStatsAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        const string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Server}/{AzuraApiEndpoints.Stats}";
        AzuraHardwareStatsModel? stats = await GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraHardwareStatsModel, CreateHeader(apiKey));
        if (stats is null)
            return null;

        stats.Ping = await _webService.GetPingAsync(baseUrl);

        return stats;
    }

    public Task<AzuraStatusModel?> GetInstanceStatusAsync(Uri baseUrl)
    {
        const string endpoint = AzuraApiEndpoints.Status;

        return GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraStatusModel, noLogging: true);
    }

    public Task<AzuraNowPlayingDataModel?> GetNowPlayingAsync(Uri baseUrl, string apiKey, int stationId, bool noLogging = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.NowPlaying}/{stationId}";

        return GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraNowPlayingDataModel, CreateHeader(apiKey), noLogging);
    }

    public Task<AzuraPlaylistModel?> GetPlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlist}/{playlistId}";

        return GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraPlaylistModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraPlaylistModel>?> GetPlaylistsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlists}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraPlaylistModel, CreateHeader(apiKey));
    }

    public async Task<IEnumerable<AzuraPlaylistModel>?> GetPlaylistsWithRequestsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        IEnumerable<AzuraPlaylistModel>? playlists = await GetPlaylistsAsync(baseUrl, apiKey, stationId);

        return playlists?.Where(static p => p.IncludeInRequests);
    }

    public async Task<AzuraRequestModel?> GetRequestableSongAsync(Uri baseUrl, string apiKey, int stationId, string? songId = null, string? name = null, string? artist = null, string? album = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        IEnumerable<AzuraRequestModel>? songs = await GetRequestableSongsAsync(baseUrl, apiKey, stationId);

        return songs?.FirstOrDefault(s =>
            (songId is null || s.Song.SongId == songId) &&
            (name is null || s.Song.Title == name) &&
            (artist is null || s.Song.Artist == artist) &&
            (album is null || s.Song.Album == album)
            );
    }

    public Task<IEnumerable<AzuraRequestModel>?> GetRequestableSongsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Requests}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraRequestModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraMediaItemModel>?> GetSongsInPlaylistAsync(Uri baseUrl, string apiKey, int stationId, AzuraPlaylistModel playlist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentNullException.ThrowIfNull(playlist);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}/{AzuraApiFilters.List}?{AzuraApiFilters.SearchPhrase}={AzuraApiFilters.Playlist}:{playlist.ShortName}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraMediaItemModel, CreateHeader(apiKey));
    }

    public async Task<AzuraSongDataModel?> GetSongInfoAsync(Uri baseUrl, string apiKey, AzuraCastStationEntity station, bool online, string? uniqueId = null, string? songId = null, string? name = null, string? artist = null, string? album = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(station);

        IEnumerable<AzuraFilesModel>? songs = (online)
            ? await GetFilesOnlineBasicAsync(baseUrl, apiKey, station.StationId)
            : await GetFilesLocalAsync(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);

        if (songs is null)
            return null;

        AzuraFilesModel? song = songs.FirstOrDefault(s =>
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

    public Task<AzuraStationModel?> GetStationAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}";

        return GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraStationModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraAdminStationConfigModel>?> GetStationsAdminConfigAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Stations}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraAdminStationConfigModel, CreateHeader(apiKey));
    }

    public Task<AzuraAdminStationConfigModel?> GetStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        return GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraAdminStationConfigModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraStationHistoryItemModel>?> GetStationHistoryAsync(Uri baseUrl, string apiKey, int stationId, in DateTimeOffset startHistory, in DateTimeOffset endHistory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.History}?{AzuraApiFilters.Start}={startHistory:yyyy-MM-dd}&{AzuraApiFilters.End}={endHistory:yyyy-MM-dd}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraStationHistoryItemModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraStationListenerModel>?> GetStationListenersAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Listeners}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraStationListenerModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraHlsMountModel>?> GetStationHlsMountPointsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.HlsStreams}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraHlsMountModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraStationQueueItemDetailedModel>?> GetStationQueueAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Queue}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraStationQueueItemDetailedModel, CreateHeader(apiKey));
    }

    public Task<IEnumerable<AzuraRequestQueueItemModel>?> GetStationRequestItemsAsync(Uri baseUrl, string apiKey, int stationId, bool history)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);

        string endpoint = (history)
            ? $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}?{AzuraApiFilters.Type}={AzuraApiFilters.History}"
            : $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}?{AzuraApiFilters.Type}={AzuraApiFilters.Pending}";

        return GetFromApiListAsync(baseUrl, endpoint, JsonSourceGen.Default.IEnumerableAzuraRequestQueueItemModel, CreateHeader(apiKey));
    }

    public async Task<AzuraSystemLogModel?> GetSystemLogAsync(Uri baseUrl, string apiKey, string logName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(logName);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Log}/{logName}";

        try
        {
            return await GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraSystemLogModel, CreateHeader(apiKey));
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public Task<AzuraSystemLogsModel?> GetSystemLogsAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Logs}";

        return GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraSystemLogsModel, CreateHeader(apiKey));
    }

    public Task<string?> GetUpdatesAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}";

        return GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.String, CreateHeader(apiKey));
    }

    public async Task ModifyStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId, AzuraAdminStationConfigModel config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentNullException.ThrowIfNull(config);

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        await PutToApiAsync(baseUrl, endpoint, JsonSerializer.Serialize(config, JsonSourceGen.Default.AzuraAdminStationConfigModel), CreateHeader(apiKey));
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

        AzuraInternalRequestModel songRequest = new(songPath[..lastSlash], AzuraApiEndpoints.Queue, [songPath]);

        await PutToApiAsync(baseUrl, endpoint, JsonSerializer.Serialize(songRequest, JsonSourceGen.Default.AzuraInternalRequestModel), CreateHeader(apiKey));
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
        AzuraAdminStationConfigModel? config = await GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraAdminStationConfigModel, CreateHeader(apiKey));
        if (config is null)
            return false;

        config.IsEnabled = true;
        await ModifyStationAdminConfigAsync(baseUrl, apiKey, stationId, config);

        await context.EditResponseAsync("I activated the station, please wait for it to start up.");
        await Task.Delay(TimeSpan.FromSeconds(10));

        endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Status}";
        AzuraStationStatusModel? status = await GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraStationStatusModel, CreateHeader(apiKey));
        if (status is null)
            return false;

        if (!status.BackendRunning || !status.FrontendRunning)
            throw new InvalidOperationException("Station failed to start.");

        if (!config.BackendConfig.WritePlaylistsToLiquidsoap)
            return true;

        // Delay to ensure the station is fully started
        await Task.Delay(TimeSpan.FromSeconds(5));

        AzuraNowPlayingDataModel? nowPlaying = null;
        bool? firstTime = null;
        while (nowPlaying is null)
        {
            try
            {
                nowPlaying = await GetNowPlayingAsync(baseUrl, apiKey, stationId, noLogging: true);
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

        AzuraAdminStationConfigModel? config = await GetFromApiAsync(baseUrl, endpoint, JsonSourceGen.Default.AzuraAdminStationConfigModel, CreateHeader(apiKey));
        if (config is null)
            return false;

        config.IsEnabled = false;

        await ModifyStationAdminConfigAsync(baseUrl, apiKey, stationId, config);

        return true;
    }

    public async Task<List<AzuraPlaylistStateModel>?> SwitchPlaylistsAsync(Uri baseUrl, string apiKey, int stationId, int playlistId, bool removeOld)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId);

        IEnumerable<AzuraPlaylistModel>? playlists = await GetPlaylistsAsync(baseUrl, apiKey, stationId);
        if (playlists is null)
            return null;

        List<AzuraPlaylistStateModel> states = new(1);

        if (removeOld)
        {
            foreach (AzuraPlaylistModel playlist in playlists.Where(p => p.IsEnabled && p.Id != playlistId))
            {
                await TogglePlaylistAsync(baseUrl, apiKey, stationId, playlist.Id);
                states.Add(new(playlist.Name, !playlist.IsEnabled));
            }
        }

        AzuraPlaylistModel current = playlists.FirstOrDefault(p => p.Id == playlistId) ?? throw new InvalidOperationException($"Playlist with id {playlistId} not found.");
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
        AzuraStatusModel? status;
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

    public async Task<T?> UploadFileAsync<T>(Uri baseUrl, string apiKey, int stationId, string file, string fileName, string filePath, JsonTypeInfo<T> jsonType)
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
            return JsonSerializer.Deserialize(result, jsonType);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize result: {result}", ex);
        }
    }
}
