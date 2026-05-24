using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Data.Entities;

using DSharpPlus.Commands.Processors.SlashCommands;

namespace AzzyBot.Bot.Services.Modules.Interfaces;

public interface IAzuraCastApiService
{
    string AzuraCastPermissionsWiki { get; }
    string FilePath { get; }
    Task CheckForApiPermissionsAsync(AzuraCastEntity azuraCast);
    Task CheckForApiPermissionsAsync(AzuraCastStationEntity station);
    Task DeleteStationSongRequestAsync(Uri baseUrl, string apiKey, int stationId, int requestId = 0);
    Task DownloadPlaylistAsync(Uri url, string apiKey, string downloadPath);
    Task<string> DownloadSongArtworkAsync(Uri url, string apiKey, string downloadPath);
    Task<IEnumerable<AzuraFilesRecord>> GetFilesLocalAsync(int guildId, int azuraCastId, int databaseId, int stationId);
    Task<IEnumerable<AzuraFilesRecord>?> GetFilesOnlineBasicAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraFilesDetailedRecord>?> GetFilesOnlineDetailedAsync(Uri baseUrl, string apiKey, int stationId);
    Task<AzuraHardwareStatsRecord?> GetHardwareStatsAsync(Uri baseUrl, string apiKey);
    Task<AzuraStatusRecord?> GetInstanceStatusAsync(Uri baseUrl);
    Task<AzuraNowPlayingDataRecord?> GetNowPlayingAsync(Uri baseUrl, string apiKey, int stationId, bool noLogging = false);
    Task<AzuraPlaylistRecord?> GetPlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId);
    Task<IEnumerable<AzuraPlaylistRecord>?> GetPlaylistsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraPlaylistRecord>?> GetPlaylistsWithRequestsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<AzuraRequestRecord?> GetRequestableSongAsync(Uri baseUrl, string apiKey, int stationId, string? songId = null, string? name = null, string? artist = null, string? album = null);
    Task<IEnumerable<AzuraRequestRecord>?> GetRequestableSongsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraMediaItemRecord>?> GetSongsInPlaylistAsync(Uri baseUrl, string apiKey, int stationId, AzuraPlaylistRecord playlist);
    Task<AzuraSongDataRecord?> GetSongInfoAsync(Uri baseUrl, string apiKey, AzuraCastStationEntity station, bool online, string? uniqueId = null, string? songId = null, string? name = null, string? artist = null, string? album = null);
    Task<AzuraStationRecord?> GetStationAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraAdminStationConfigRecord>?> GetStationsAdminConfigAsync(Uri baseUrl, string apiKey);
    Task<AzuraAdminStationConfigRecord?> GetStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraStationHistoryItemRecord>?> GetStationHistoryAsync(Uri baseUrl, string apiKey, int stationId, in DateTimeOffset startHistory, in DateTimeOffset endHistory);
    Task<IEnumerable<AzuraHlsMountRecord>?> GetStationHlsMountPointsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraStationListenerRecord>?> GetStationListenersAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraStationQueueItemDetailedRecord>?> GetStationQueueAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraRequestQueueItemRecord>?> GetStationRequestItemsAsync(Uri baseUrl, string apiKey, int stationId, bool history);
    Task<AzuraSystemLogRecord?> GetSystemLogAsync(Uri baseUrl, string apiKey, string logName);
    Task<AzuraSystemLogsRecord?> GetSystemLogsAsync(Uri baseUrl, string apiKey);
    Task<string?> GetUpdatesAsync(Uri baseUrl, string apiKey);
    Task ModifyStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId, AzuraAdminStationConfigRecord config);
    Task RequestInternalSongAsync(Uri baseUrl, string apiKey, int stationId, string songPath);
    Task RequestSongAsync(Uri baseUrl, int stationId, string requestId);
    Task SkipSongAsync(Uri baseUrl, string apiKey, int stationId);
    Task<bool> StartStationAsync(Uri baseUrl, string apiKey, int stationId, SlashCommandContext context);
    Task<bool> StopStationAsync(Uri baseUrl, string apiKey, int stationId);
    Task<List<AzuraPlaylistStateRecord>?> SwitchPlaylistsAsync(Uri baseUrl, string apiKey, int stationId, int playlistId, bool removeOld);
    Task TogglePlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId);
    Task UpdateInstanceAsync(Uri baseUrl, string apiKey);
    Task<T?> UploadFileAsync<T>(Uri baseUrl, string apiKey, int stationId, string file, string fileName, string filePath, JsonTypeInfo<T> jsonType);
}
