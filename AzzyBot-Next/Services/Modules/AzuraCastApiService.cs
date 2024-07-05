using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Helpers;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Commands;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastApiService(ILogger<AzuraCastApiService> logger, DbActions dbActions, DiscordBotService botService, WebRequestService webService)
{
    private readonly ILogger<AzuraCastApiService> _logger = logger;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;
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

    private async ValueTask CheckForApiPermissionsAsync(AzuraCastEntity azuraCast)
    {
        await CheckForAdminApiPermissionsAsync(azuraCast);
        foreach (AzuraCastStationEntity station in azuraCast.Stations)
        {
            await CheckForStationApiPermissionsAsync(station);
        }
    }

    private async ValueTask CheckForApiPermissionsAsync(AzuraCastStationEntity station)
        => await CheckForStationApiPermissionsAsync(station);

    private async ValueTask CheckForAdminApiPermissionsAsync(AzuraCastEntity azuraCast)
    {
        string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
        List<Uri> apis = [];
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Server}/{AzuraApiEndpoints.Stats}"));
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Stations}"));

        if (azuraCast.Checks.Updates)
            apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}"));

        IReadOnlyList<string> missing = await ExecuteApiPermissionCheckAsync(apis, Crypto.Decrypt(azuraCast.AdminApiKey));
        if (missing.Count is 0)
            return;

        StringBuilder builder = new();
        builder.AppendLine("I can't access the following administrative endpoints:");
        foreach (string api in missing)
        {
            builder.AppendLine(api);
        }

        builder.AppendLine("Please review your permission set.");

        await _botService.SendMessageAsync(azuraCast.NotificationChannelId, builder.ToString());
    }

    private async ValueTask CheckForStationApiPermissionsAsync(AzuraCastStationEntity station)
    {
        string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
        int stationId = station.StationId;
        List<Uri> apis = [];
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.History}"));
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlists}"));
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Queue}"));
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}"));
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Requests}"));
        apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Status}"));

        if (station.Checks.FileChanges)
            apis.Add(new($"{baseUrl}/api/{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}"));

        string apiKey = (string.IsNullOrWhiteSpace(station.ApiKey)) ? station.AzuraCast.AdminApiKey : station.ApiKey;
        IReadOnlyList<string> missing = await ExecuteApiPermissionCheckAsync(apis, Crypto.Decrypt(apiKey));
        if (missing.Count == 0)
            return;

        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"I can't access the following endpoints for station **{Crypto.Decrypt(station.Name)}**:");
        foreach (string api in missing)
        {
            builder.AppendLine(api);
        }

        builder.AppendLine("Please review your permission set.");

        await _botService.SendMessageAsync(station.AzuraCast.NotificationChannelId, builder.ToString());
    }

    private async ValueTask<IReadOnlyList<string>> ExecuteApiPermissionCheckAsync(IReadOnlyList<Uri> apis, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(apis, nameof(apis));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(apis.Count, nameof(apis));

        IReadOnlyList<bool> checks = await _webService.CheckForApiPermissionsAsync(apis, CreateHeader(apiKey));
        List<string> missing = [];
        for (int i = 0; i < checks.Count; i++)
        {
            if (!checks[i])
                missing.Add(apis[i].AbsolutePath);
        }

        return missing;
    }

    private async Task DeleteToApiAsync(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        try
        {
            await _webService.DeleteAsync(uri, headers, true);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed DELETE to API, url: {uri}", ex);
        }
    }

    private async Task<string> GetFromApiAsync(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri url = new($"{baseUrl}api/{endpoint}");
        string body;
        try
        {
            body = await _webService.GetWebAsync(url, headers, true);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed GET from API, url: {url}", ex);
        }

        return body;
    }

    private async Task<T> GetFromApiAsync<T>(Uri baseUrl, string endpoint, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        string body = await _webService.GetWebAsync(uri, headers, true);
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

    private string GetLocalFile(int databaseId, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(databaseId, nameof(databaseId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        IReadOnlyList<string> files = FileOperations.GetFilesInDirectory(FilePath);
        string fileName = $"{databaseId}-{stationId}-files.json";

        return files.FirstOrDefault(f => f.Contains(fileName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private async Task PostToApiAsync(Uri baseUrl, string endpoint, string? content = null, Dictionary<string, string>? headers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        try
        {
            await _webService.PostWebAsync(uri, content, headers, true);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed POST to API, url: {uri}", ex);
        }
    }

    private async Task PutToApiAsync(Uri baseUrl, string endpoint, string? content = null, Dictionary<string, string>? headers = null, bool ignoreException = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        Uri uri = new($"{baseUrl}api/{endpoint}");
        try
        {
            await _webService.PutWebAsync(uri, content, headers, true);
        }
        catch (HttpRequestException ex)
        {
            if (ignoreException)
                return;

            throw new InvalidOperationException($"Failed PUT to API, url: {uri}", ex);
        }
    }

    public async Task QueueApiPermissionChecksAsync()
    {
        _logger.BackgroundServiceWorkItem(nameof(QueueApiPermissionChecksAsync));

        List<GuildsEntity> guilds = await _dbActions.GetGuildsAsync();
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.IsOnline == true).Select(g => g.AzuraCast!))
        {
            _ = Task.Run(async () => await CheckForApiPermissionsAsync(azuraCast));
        }
    }

    public async Task QueueApiPermissionChecksAsync(ulong guildId, int stationId = 0)
    {
        _logger.BackgroundServiceWorkItem(nameof(QueueApiPermissionChecksAsync));

        GuildsEntity? guild = await _dbActions.GetGuildAsync(guildId);
        if (guild is null || guild.AzuraCast is null)
            return;

        IEnumerable<AzuraCastStationEntity> stations = guild.AzuraCast.Stations;
        if (stationId is not 0)
        {
            AzuraCastStationEntity? station = stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
                return;

            _ = Task.Run(async () => await CheckForApiPermissionsAsync(station));
        }
        else
        {
            _ = Task.Run(async () => await CheckForApiPermissionsAsync(guild.AzuraCast));
        }
    }

    public async Task DeleteStationSongRequestAsync(Uri baseUrl, string apiKey, int stationId, int requestId = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}/" + ((requestId is 0) ? requestId : AzuraApiEndpoints.Clear);

        await DeleteToApiAsync(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task DownloadPlaylistAsync(Uri url, string apiKey, string downloadPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadPath, nameof(downloadPath));

        await _webService.DownloadAsync(url, downloadPath, CreateHeader(apiKey), true);
    }

    public async Task<IReadOnlyList<AzuraFilesRecord>> GetFilesLocalAsync(int databaseId, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        List<AzuraFilesRecord> records = [];
        string file = GetLocalFile(databaseId, stationId);
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

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}";

        return GetFromApiListAsync<AzuraFilesRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task<AzuraHardwareStatsRecord> GetHardwareStatsAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        const string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Server}/{AzuraApiEndpoints.Stats}";
        AzuraHardwareStatsRecord stats = await GetFromApiAsync<AzuraHardwareStatsRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        stats.Ping = await _webService.GetPingAsync(baseUrl);

        return stats;
    }

    public Task<AzuraNowPlayingDataRecord> GetNowPlayingAsync(Uri baseUrl, int stationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.NowPlaying}/{stationId}";

        return GetFromApiAsync<AzuraNowPlayingDataRecord>(baseUrl, endpoint);
    }

    public Task<AzuraPlaylistRecord> GetPlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlist}/{playlistId}";

        return GetFromApiAsync<AzuraPlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IReadOnlyList<AzuraPlaylistRecord>> GetPlaylistsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlists}";

        return GetFromApiListAsync<AzuraPlaylistRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task<AzuraRequestRecord> GetRequestableSongAsync(Uri baseUrl, string apiKey, int stationId, string? songId = null, string? name = null, string? artist = null, string? album = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        IReadOnlyList<AzuraRequestRecord> songs = await GetRequestableSongsAsync(baseUrl, apiKey, stationId);
        AzuraRequestRecord? song = songs.FirstOrDefault(s =>
            (songId is null || s.Song.SongId == songId) &&
            (name is null || s.Song.Title == name) &&
            (artist is null || s.Song.Artist == artist) &&
            (album is null || s.Song.Album == album)
            );

        return song ?? throw new InvalidOperationException($"Song {name} not found.");
    }

    public Task<IReadOnlyList<AzuraRequestRecord>> GetRequestableSongsAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Requests}";

        return GetFromApiListAsync<AzuraRequestRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IReadOnlyList<AzuraMediaItemRecord>> GetSongsInPlaylistAsync(Uri baseUrl, string apiKey, int stationId, AzuraPlaylistRecord playlist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentNullException.ThrowIfNull(playlist, nameof(playlist));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Files}/{AzuraApiFilters.List}?{AzuraApiFilters.SearchPhrase}={AzuraApiFilters.Playlist}:{playlist.ShortName}";

        return GetFromApiListAsync<AzuraMediaItemRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task<AzuraSongDataRecord> GetSongInfoAsync(Uri baseUrl, string apiKey, int databaseId, int stationId, bool online, string? uniqueId = null, string? songId = null, string? name = null, string? artist = null, string? album = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(databaseId, nameof(databaseId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        IReadOnlyList<AzuraFilesRecord> songs = (online) ? await GetFilesOnlineAsync(baseUrl, apiKey, stationId) : await GetFilesLocalAsync(databaseId, stationId);
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
            Art = song.Art
        };
    }

    public Task<AzuraAdminStationConfigRecord> GetStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        return GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IReadOnlyList<AzuraStationHistoryItemRecord>> GetStationHistoryAsync(Uri baseUrl, string apiKey, int stationId, in DateTime start, in DateTime end)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.History}?{AzuraApiFilters.Start}={start:yyyy-MM-dd}&{AzuraApiFilters.End}={end:yyyy-MM-dd}";

        return GetFromApiListAsync<AzuraStationHistoryItemRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IReadOnlyList<AzuraStationQueueItemDetailedRecord>> GetStationQueueAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Queue}";

        return GetFromApiListAsync<AzuraStationQueueItemDetailedRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<IReadOnlyList<AzuraRequestQueueItemRecord>> GetStationRequestItemsAsync(Uri baseUrl, string apiKey, int stationId, bool history)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = (history)
            ? $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}?{AzuraApiFilters.Type}={AzuraApiFilters.History}"
            : $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Reports}/{AzuraApiEndpoints.Requests}?{AzuraApiFilters.Type}={AzuraApiFilters.Pending}";

        return GetFromApiListAsync<AzuraRequestQueueItemRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public Task<AzuraUpdateRecord> GetUpdatesAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}";

        return GetFromApiAsync<AzuraUpdateRecord>(baseUrl, endpoint, CreateHeader(apiKey));
    }

    public async Task ModifyStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId, AzuraAdminStationConfigRecord config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        await PutToApiAsync(baseUrl, endpoint, JsonSerializer.Serialize(config, _jsonOptions), CreateHeader(apiKey));
    }

    public async Task RequestSongAsync(Uri baseUrl, int stationId, string songId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(songId, nameof(songId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Request}/{songId}";

        await PostToApiAsync(baseUrl, endpoint);
    }

    public async Task SkipSongAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Backend}/{AzuraApiEndpoints.Skip}";

        await PostToApiAsync(baseUrl, endpoint, null, CreateHeader(apiKey));
    }

    public async Task StartStationAsync(Uri baseUrl, string apiKey, int stationId, CommandContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";
        AzuraAdminStationConfigRecord config = await GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        config.IsEnabled = true;
        await ModifyStationAdminConfigAsync(baseUrl, apiKey, stationId, config);

        await context.EditResponseAsync("I activated the station, please wait for it to start up.");
        await Task.Delay(TimeSpan.FromSeconds(10));

        endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Status}";
        AzuraStationStatusRecord status = await GetFromApiAsync<AzuraStationStatusRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        if (!status.BackendRunning || !status.FrontendRunning)
            throw new InvalidOperationException("Station failed to start.");
    }

    public async Task StopStationAsync(Uri baseUrl, string apiKey, int stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Station}/{stationId}";

        AzuraAdminStationConfigRecord config = await GetFromApiAsync<AzuraAdminStationConfigRecord>(baseUrl, endpoint, CreateHeader(apiKey));
        config.IsEnabled = false;

        await ModifyStationAdminConfigAsync(baseUrl, apiKey, stationId, config);
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

        string endpoint = $"{AzuraApiEndpoints.Station}/{stationId}/{AzuraApiEndpoints.Playlist}/{playlistId}/{AzuraApiEndpoints.Toggle}";

        await PutToApiAsync(baseUrl, endpoint, null, CreateHeader(apiKey));
    }

    public async Task UpdateInstanceAsync(Uri baseUrl, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        string endpoint = $"{AzuraApiEndpoints.Admin}/{AzuraApiEndpoints.Updates}";

        await PutToApiAsync(baseUrl, endpoint, null, CreateHeader(apiKey), true);

        bool online = false;
        AzuraStatusRecord status;
        while (!online)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));

            try
            {
                status = await GetFromApiAsync<AzuraStatusRecord>(baseUrl, AzuraApiEndpoints.Status);
                online = status.Online == "true";
            }
            catch (InvalidOperationException)
            {
                online = false;
            }
        }
    }
}
