using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;

namespace AzzyBot.Data.Services.Interfaces;

public interface IDbActions
{
    Task CreateAzuraCastAsync(ulong guildId, Uri baseUrl, string apiKey, ulong instanceAdminGroup, ulong notificationId, ulong outagesId, bool serverStatus, bool updates, bool changelog);
    Task CreateAzuraCastStationAsync(ulong guildId, int stationId, ulong stationAdminGroup, ulong requestsId, bool showPlaylist, bool fileChanges, ulong? fileUploadId = null, string? fileUploadPath = null, string? apiKey = null, ulong? stationDjGroup = null);
    Task CreateAzuraCastStationRequestAsync(ulong guildId, int stationId, string songId, bool isInternal = false);
    Task CreateGuildAsync(ulong guildId);
    Task<IEnumerable<DiscordGuild>> CreateGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds);
    Task CreateMusicStreamingAsync(ulong guildId, ulong nowPlayingEmbedChannelId = 0, ulong nowPlayingEmbedMessageId = 0, int volume = 50);
    Task DeleteAzuraCastAsync(ulong guildId);
    Task DeleteAzuraCastStationAsync(int stationId);
    Task DeleteGuildAsync(ulong guildId);
    Task<IEnumerable<ulong>> DeleteGuildsAsync(IAsyncEnumerable<DiscordGuild> guilds);
    Task DeleteMusicStreamingAsync(ulong guildId);
    Task<AzuraCastEntity?> ReadAzuraCastAsync(ulong guildId, bool loadChecks = false, bool loadPrefs = false, bool loadStations = false, bool loadStationChecks = false, bool loadStationPrefs = false, bool loadGuild = false);
    Task<IReadOnlyList<AzuraCastEntity>> ReadAzuraCastsAsync(bool loadChecks = false, bool loadPrefs = false, bool loadStations = false, bool loadStationChecks = false, bool loadStationPrefs = false, bool loadGuild = false);
    Task<AzuraCastStationEntity?> ReadAzuraCastStationAsync(ulong guildId, int stationId, bool loadChecks = false, bool loadPrefs = false, bool loadRequests = false, bool loadAzuraCast = false, bool loadAzuraCastPrefs = false);
    Task<AzuraCastStationPreferencesEntity?> ReadAzuraCastStationPreferencesAsync(ulong guildId, int stationId, bool loadStation = false);
    Task<int> ReadAzuraCastStationRequestsCountAsync(ulong guildId, int stationId);
    Task<AzzyBotEntity?> ReadAzzyBotAsync();
    Task<GuildEntity?> ReadGuildAsync(ulong guildId, bool loadEverything = false);
    Task<IReadOnlyList<GuildEntity>> ReadGuildsAsync(bool loadGuildPrefs = false, bool loadEverything = false);
    Task<GuildPreferencesEntity?> ReadGuildPreferencesAsync(ulong guildId);
    Task<MusicStreamingEntity?> ReadMusicStreamingAsync(ulong guildId, bool loadGuild = false);
    Task<IReadOnlyList<MusicStreamingEntity>> ReadMusicStreamingAsync(bool loadGuild = false);
    Task UpdateAzuraCastAsync(ulong guildId, Uri? baseUrl = null, string? apiKey = null, bool? isOnline = null);
    Task UpdateAzuraCastChecksAsync(ulong guildId, bool? serverStatus = null, bool? updates = null, bool? changelog = null, int? updateNotificationCounter = null, bool? lastUpdateCheck = null, bool? lastServerStatusCheck = null);
    Task UpdateAzuraCastPreferencesAsync(ulong guildId, ulong? instanceAdminGroup = null, ulong? notificationId = null, ulong? outagesId = null);
    Task UpdateAzuraCastStationAsync(ulong guildId, int station, int? stationId = null, string? apiKey = null, bool? lastSkipTime = null, bool? lastRequestTime = null);
    Task UpdateAzuraCastStationChecksAsync(ulong guildId, int stationId, bool? fileChanges = null, bool? lastFileChangesCheck = null);
    Task UpdateAzuraCastStationPreferencesAsync(ulong guildId, int stationId, ulong? stationAdminGroup = null, ulong? stationDjGroup = null, ulong? fileUploadId = null, ulong? nowPlayingEmbedChannelId = null, ulong? nowPlayingEmbedMessageId = null, ulong? requestId = null, string? fileUploadPath = null, bool? playlist = null);
    Task UpdateAzzyBotAsync(bool? lastDatabaseCleanup = null, bool? lastGuildReminder = null, bool? lastUpdateCheck = null);
    Task UpdateGuildAsync(ulong guildId, bool? lastPermissionCheck = null, DateTimeOffset? reminderLeaveDate = null, bool? legalsAccepted = null);
    Task UpdateGuildsLegalsAsync();
    Task UpdateGuildPreferencesAsync(ulong guildId, ulong? adminRoleId = null, ulong? adminNotifyChannelId = null);
    Task UpdateMusicStreamingAsync(ulong guildId, ulong? nowPlayingEmbedChannelId = null, ulong? nowPlayingEmbedMessageId = null, int? volume = null);
}
