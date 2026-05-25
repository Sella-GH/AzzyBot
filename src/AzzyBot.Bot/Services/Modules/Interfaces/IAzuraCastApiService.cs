using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

using AzzyBot.Bot.Models.AzuraCast;
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
    Task<IEnumerable<AzuraFilesModel>> GetFilesLocalAsync(int guildId, int azuraCastId, int databaseId, int stationId);
    Task<IEnumerable<AzuraFilesModel>?> GetFilesOnlineBasicAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraFilesDetailedModel>?> GetFilesOnlineDetailedAsync(Uri baseUrl, string apiKey, int stationId);
    Task<AzuraHardwareStatsModel?> GetHardwareStatsAsync(Uri baseUrl, string apiKey);
    Task<AzuraStatusModel?> GetInstanceStatusAsync(Uri baseUrl);
    Task<AzuraNowPlayingDataModel?> GetNowPlayingAsync(Uri baseUrl, string apiKey, int stationId, bool noLogging = false);
    Task<AzuraPlaylistModel?> GetPlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId);
    Task<IEnumerable<AzuraPlaylistModel>?> GetPlaylistsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraPlaylistModel>?> GetPlaylistsWithRequestsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<AzuraRequestModel?> GetRequestableSongAsync(Uri baseUrl, string apiKey, int stationId, string? songId = null, string? name = null, string? artist = null, string? album = null);
    Task<IEnumerable<AzuraRequestModel>?> GetRequestableSongsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraMediaItemModel>?> GetSongsInPlaylistAsync(Uri baseUrl, string apiKey, int stationId, AzuraPlaylistModel playlist);
    Task<AzuraSongDataModel?> GetSongInfoAsync(Uri baseUrl, string apiKey, AzuraCastStationEntity station, bool online, string? uniqueId = null, string? songId = null, string? name = null, string? artist = null, string? album = null);
    Task<AzuraStationModel?> GetStationAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraAdminStationConfigModel>?> GetStationsAdminConfigAsync(Uri baseUrl, string apiKey);
    Task<AzuraAdminStationConfigModel?> GetStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraStationHistoryItemModel>?> GetStationHistoryAsync(Uri baseUrl, string apiKey, int stationId, in DateTimeOffset startHistory, in DateTimeOffset endHistory);
    Task<IEnumerable<AzuraHlsMountModel>?> GetStationHlsMountPointsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraStationListenerModel>?> GetStationListenersAsync(Uri baseUrl, string apiKey, int stationId);
    Task<AzuraSystemLogModel?> GetStationLogAsync(Uri baseUrl, string apiKey, int stationId, string logName);
    Task<IEnumerable<AzuraSystemLogEntryModel>?> GetStationLogsAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraStationQueueItemDetailedModel>?> GetStationQueueAsync(Uri baseUrl, string apiKey, int stationId);
    Task<IEnumerable<AzuraRequestQueueItemModel>?> GetStationRequestItemsAsync(Uri baseUrl, string apiKey, int stationId, bool history);
    Task<AzuraSystemLogModel?> GetSystemLogAsync(Uri baseUrl, string apiKey, string logName);
    Task<AzuraSystemLogsModel?> GetSystemLogsAsync(Uri baseUrl, string apiKey);
    Task<string?> GetUpdatesAsync(Uri baseUrl, string apiKey);
    Task ModifyStationAdminConfigAsync(Uri baseUrl, string apiKey, int stationId, AzuraAdminStationConfigModel config);
    Task RequestInternalSongAsync(Uri baseUrl, string apiKey, int stationId, string songPath);
    Task RequestSongAsync(Uri baseUrl, int stationId, string requestId);
    Task SkipSongAsync(Uri baseUrl, string apiKey, int stationId);
    Task<bool> StartStationAsync(Uri baseUrl, string apiKey, int stationId, SlashCommandContext context);
    Task<bool> StopStationAsync(Uri baseUrl, string apiKey, int stationId);
    Task<List<AzuraPlaylistStateModel>?> SwitchPlaylistsAsync(Uri baseUrl, string apiKey, int stationId, int playlistId, bool removeOld);
    Task TogglePlaylistAsync(Uri baseUrl, string apiKey, int stationId, int playlistId);
    Task UpdateInstanceAsync(Uri baseUrl, string apiKey);
    Task<T?> UploadFileAsync<T>(Uri baseUrl, string apiKey, int stationId, string file, string fileName, string filePath, JsonTypeInfo<T> jsonType);
}
