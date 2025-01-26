using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Extensions;

using DSharpPlus.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Data.Services;

public sealed class DbActions(ILogger<DbActions> logger, AzzyDbContext dbContext)
{
    private readonly ILogger<DbActions> _logger = logger;
    private readonly AzzyDbContext _dbContext = dbContext;

    public async Task AddAzuraCastAsync(ulong guildId, Uri baseUrl, string apiKey, ulong instanceAdminGroup, ulong notificationId, ulong outagesId, bool serverStatus, bool updates, bool changelog)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);

        GuildEntity? guild = await _dbContext.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
        if (guild is null)
        {
            _logger.DatabaseGuildNotFound(guildId);
            return;
        }

        AzuraCastEntity azuraCast = new()
        {
            BaseUrl = Crypto.Encrypt(baseUrl.OriginalString),
            AdminApiKey = Crypto.Encrypt(apiKey),
            GuildId = guild.Id
        };

        azuraCast.Checks = new()
        {
            ServerStatus = serverStatus,
            Updates = updates,
            UpdatesShowChangelog = changelog,
            AzuraCastId = azuraCast.Id
        };

        azuraCast.Preferences = new()
        {
            InstanceAdminRoleId = instanceAdminGroup,
            NotificationChannelId = notificationId,
            OutagesChannelId = outagesId,
            AzuraCastId = azuraCast.Id
        };

        await _dbContext.AzuraCast.AddAsync(azuraCast);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddAzuraCastStationAsync(ulong guildId, int stationId, ulong stationAdminGroup, ulong requestsId, bool showPlaylist, bool fileChanges, ulong? fileUploadId = null, string? fileUploadPath = null, string? apiKey = null, ulong? stationDjGroup = null)
    {
        AzuraCastEntity? azura = await _dbContext.AzuraCast.SingleOrDefaultAsync(a => a.Guild.UniqueId == guildId);
        if (azura is null)
        {
            _logger.DatabaseAzuraCastNotFound(guildId);
            return;
        }

        AzuraCastStationEntity station = new()
        {
            StationId = stationId,
            ApiKey = (string.IsNullOrEmpty(apiKey)) ? string.Empty : Crypto.Encrypt(apiKey),
            LastSkipTime = DateTimeOffset.MinValue,
            LastRequestTime = DateTimeOffset.MinValue,
            AzuraCastId = azura.Id
        };

        station.Checks = new()
        {
            FileChanges = fileChanges,
            StationId = station.Id
        };

        station.Preferences = new()
        {
            FileUploadChannelId = fileUploadId ?? 0,
            FileUploadPath = fileUploadPath ?? string.Empty,
            RequestsChannelId = requestsId,
            ShowPlaylistInNowPlaying = showPlaylist,
            StationAdminRoleId = stationAdminGroup,
            StationDjRoleId = stationDjGroup ?? 0,
            StationId = station.Id
        };

        await _dbContext.AzuraCastStations.AddAsync(station);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddAzuraCastStationRequestAsync(ulong guildId, int stationId, string songId, bool isInternal = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songId);

        AzuraCastStationEntity? station = await _dbContext.AzuraCastStations.SingleOrDefaultAsync(s => s.AzuraCast.Guild.UniqueId == guildId && s.StationId == stationId);
        if (station is null)
        {
            _logger.DatabaseAzuraCastStationNotFound(guildId, 0, stationId);
            return;
        }

        AzuraCastStationRequestEntity request = new()
        {
            SongId = songId,
            StationId = station.Id,
            Timestamp = DateTimeOffset.UtcNow,
            IsInternal = isInternal
        };

        await _dbContext.AzuraCastStationRequests.AddAsync(request);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddGuildAsync(ulong guildId)
    {
        await _dbContext.Guilds.AddAsync(new() { UniqueId = guildId });
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Checks and adds <see cref="DiscordGuild"/> to the database in which the bot is a member of.
    /// </summary>
    /// <param name="guilds">The list of <see cref="DiscordGuild"/> in which the bot is a member of.</param>
    /// <returns>An <see cref="IEnumerable{T}"/>with the added <see cref="DiscordGuild"/>s.</returns>
    public async Task<IEnumerable<DiscordGuild>> AddGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);

        IEnumerable<GuildEntity> existingGuilds = _dbContext.Guilds;
        IEnumerable<GuildEntity> newGuilds = [.. guilds.Keys
            .Where(guild => !existingGuilds.Select(static g => g.UniqueId).Contains(guild))
            .Select(static guild => new GuildEntity() { UniqueId = guild })];

        if (!newGuilds.Any())
            return [];

        await _dbContext.Guilds.AddRangeAsync(newGuilds);
        await _dbContext.SaveChangesAsync();

        return newGuilds
            .Where(guild => guilds.ContainsKey(guild.UniqueId))
            .Select(guild => guilds[guild.UniqueId]);
    }

    public async Task DeleteAzuraCastAsync(ulong guildId)
        => await _dbContext.AzuraCast.Where(a => a.Guild.UniqueId == guildId).ExecuteDeleteAsync();

    public async Task DeleteAzuraCastStationAsync(int stationId)
        => await _dbContext.AzuraCastStations.Where(s => s.StationId == stationId).ExecuteDeleteAsync();

    public async Task DeleteGuildAsync(ulong guildId)
    {
        await _dbContext.AzuraCast.Where(a => a.Guild.UniqueId == guildId).ExecuteDeleteAsync();
        await _dbContext.Guilds.Where(g => g.UniqueId == guildId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Checks and deletes <see cref="DiscordGuild"/>s from the database in which the bot is no longer a member of.
    /// </summary>
    /// <param name="guilds">The list of <see cref="DiscordGuild"/>s in which the bot is a member of.</param>
    /// <returns>An <see cref="IEnumerable{T}"/>with the deleted <see cref="DiscordGuild"/> ids as a <see langword="ulong"/>.</returns>
    public async Task<IEnumerable<ulong>> DeleteGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);

        IEnumerable<GuildEntity> existingGuilds = _dbContext.Guilds;
        IEnumerable<GuildEntity> guildsToDelete = [.. existingGuilds.Where(guild => !guilds.Keys.Contains(guild.UniqueId))];

        if (!guildsToDelete.Any())
            return [];

        await _dbContext.AzuraCast.Where(a => guildsToDelete.Select(static g => g.UniqueId).Contains(a.Guild.UniqueId)).ExecuteDeleteAsync();
        await _dbContext.Guilds.Where(g => guildsToDelete.Select(static g => g.UniqueId).Contains(g.UniqueId)).ExecuteDeleteAsync();

        return guildsToDelete
            .Where(guild => !guilds.ContainsKey(guild.UniqueId))
            .Select(static guld => guld.UniqueId);
    }

    public Task<AzuraCastEntity?> GetAzuraCastAsync(ulong guildId, bool loadChecks = false, bool loadPrefs = false, bool loadStations = false, bool loadStationChecks = false, bool loadStationPrefs = false, bool loadGuild = false)
    {
        return _dbContext.AzuraCast
            .AsNoTracking()
            .Where(a => a.Guild.UniqueId == guildId)
            .IncludeIf(loadChecks, static q => q.Include(static a => a.Checks))
            .IncludeIf(loadPrefs, static q => q.Include(static a => a.Preferences))
            .IncludeIf(loadStations, static q => q.Include(static a => a.Stations))
            .IncludeIf(loadStations && loadStationChecks, static q => q.Include(static a => a.Stations).ThenInclude(static s => s.Checks))
            .IncludeIf(loadStations && loadStationPrefs, static q => q.Include(static a => a.Stations).ThenInclude(static s => s.Preferences))
            .IncludeIf(loadGuild, static q => q.Include(static a => a.Guild))
            .SingleOrDefaultAsync();
    }

    public Task<AzuraCastStationEntity?> GetAzuraCastStationAsync(ulong guildId, int stationId, bool loadChecks = false, bool loadPrefs = false, bool loadRequests = false, bool loadAzuraCast = false, bool loadAzuraCastPrefs = false)
    {
        return _dbContext.AzuraCastStations
            .AsNoTracking()
            .Where(s => s.AzuraCast.Guild.UniqueId == guildId && s.StationId == stationId)
            .IncludeIf(loadChecks, static q => q.Include(static s => s.Checks))
            .IncludeIf(loadPrefs, static q => q.Include(static s => s.Preferences))
            .IncludeIf(loadRequests, static q => q.Include(static s => s.Requests))
            .IncludeIf(loadAzuraCast, static q => q.Include(static s => s.AzuraCast))
            .IncludeIf(loadAzuraCastPrefs, static q => q.Include(static s => s.AzuraCast.Preferences))
            .SingleOrDefaultAsync();
    }

    public Task<AzuraCastStationPreferencesEntity?> GetAzuraCastStationPreferencesAsync(ulong guildId, int stationId, bool loadStation = false)
    {
        return _dbContext.AzuraCastStationPreferences
            .AsNoTracking()
            .Where(p => p.Station.AzuraCast.Guild.UniqueId == guildId && p.Station.StationId == stationId)
            .IncludeIf(loadStation, static q => q.Include(static p => p.Station))
            .SingleOrDefaultAsync();
    }

    public Task<int> GetAzuraCastStationRequestsCountAsync(ulong guildId, int stationId)
    {
        return _dbContext.AzuraCastStationRequests
            .AsNoTracking()
            .Where(r => r.Station.AzuraCast.Guild.UniqueId == guildId && r.Station.StationId == stationId)
            .CountAsync();
    }

    public Task<AzzyBotEntity?> GetAzzyBotAsync()
    {
        return _dbContext.AzzyBot
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }

    public Task<GuildEntity?> GetGuildAsync(ulong guildId, bool loadEverything = false)
    {
        return _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .IncludeIf(loadEverything, static q => q.Include(static g => g.Preferences))
            .IncludeIf(loadEverything, static q => q.Include(static g => g.AzuraCast).Include(static g => g.AzuraCast!.Checks).Include(static g => g.AzuraCast!.Preferences))
            .IncludeIf(loadEverything, static q => q.Include(static g => g.AzuraCast!.Stations).ThenInclude(static s => s.Checks))
            .IncludeIf(loadEverything, static q => q.Include(static g => g.AzuraCast!.Stations).ThenInclude(static s => s.Preferences))
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<GuildEntity>> GetGuildsAsync(bool loadGuildPrefs = false, bool loadEverything = false)
    {
        return await _dbContext.Guilds
            .AsNoTracking()
            .IncludeIf(loadGuildPrefs || loadEverything, static q => q.Include(static g => g.Preferences))
            .IncludeIf(loadEverything, static q => q.Include(static g => g.AzuraCast).Include(static g => g.AzuraCast!.Checks).Include(static g => g.AzuraCast!.Preferences))
            .IncludeIf(loadEverything, static q => q.Include(static g => g.AzuraCast!.Stations).ThenInclude(static s => s.Checks))
            .IncludeIf(loadEverything, static q => q.Include(static g => g.AzuraCast!.Stations).ThenInclude(static s => s.Preferences))
            .ToListAsync();
    }

    public Task<GuildPreferencesEntity?> GetGuildPreferencesAsync(ulong guildId)
    {
        return _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .Select(static g => g.Preferences)
            .SingleOrDefaultAsync();
    }

    public async Task UpdateAzuraCastAsync(ulong guildId, Uri? baseUrl = null, string? apiKey = null, bool? isOnline = null)
    {
        AzuraCastEntity? azuraCast = await _dbContext.AzuraCast
            .Where(a => a.Guild.UniqueId == guildId)
            .SingleOrDefaultAsync();

        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(guildId);
            return;
        }

        if (baseUrl is not null)
            azuraCast.BaseUrl = Crypto.Encrypt(baseUrl.OriginalString);

        if (!string.IsNullOrEmpty(apiKey))
            azuraCast.AdminApiKey = Crypto.Encrypt(apiKey);

        if (isOnline.HasValue)
            azuraCast.IsOnline = isOnline.Value;

        _dbContext.AzuraCast.Update(azuraCast);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAzuraCastChecksAsync(ulong guildId, bool? serverStatus = null, bool? updates = null, bool? changelog = null, int? updateNotificationCounter = null, bool? lastUpdateCheck = null, bool? lastServerStatusCheck = null)
    {
        AzuraCastChecksEntity? checks = await _dbContext.AzuraCastChecks
            .Where(c => c.AzuraCast.Guild.UniqueId == guildId)
            .SingleOrDefaultAsync();

        if (checks is null)
        {
            _logger.DatabaseAzuraCastChecksNotFound(guildId, 0);
            return;
        }

        if (serverStatus.HasValue)
            checks.ServerStatus = serverStatus.Value;

        if (updates.HasValue)
            checks.Updates = updates.Value;

        if (changelog.HasValue)
            checks.UpdatesShowChangelog = changelog.Value;

        if (updateNotificationCounter.HasValue)
            checks.UpdateNotificationCounter = updateNotificationCounter.Value;

        if (lastUpdateCheck.HasValue)
            checks.LastUpdateCheck = DateTimeOffset.UtcNow;

        if (lastServerStatusCheck.HasValue)
            checks.LastServerStatusCheck = DateTimeOffset.UtcNow;

        _dbContext.AzuraCastChecks.Update(checks);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAzuraCastPreferencesAsync(ulong guildId, ulong? instanceAdminGroup = null, ulong? notificationId = null, ulong? outagesId = null)
    {
        AzuraCastPreferencesEntity? preferences = await _dbContext.AzuraCastPreferences
            .Where(p => p.AzuraCast.Guild.UniqueId == guildId)
            .SingleOrDefaultAsync();

        if (preferences is null)
        {
            _logger.DatabaseAzuraCastPreferencesNotFound(guildId, 0);
            return;
        }

        if (instanceAdminGroup.HasValue)
            preferences.InstanceAdminRoleId = instanceAdminGroup.Value;

        if (notificationId.HasValue)
            preferences.NotificationChannelId = notificationId.Value;

        if (outagesId.HasValue)
            preferences.OutagesChannelId = outagesId.Value;

        _dbContext.AzuraCastPreferences.Update(preferences);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAzuraCastStationAsync(ulong guildId, int station, int? stationId = null, string? apiKey = null, bool? lastSkipTime = null, bool? lastRequestTime = null)
    {
        AzuraCastStationEntity? azuraStation = await _dbContext.AzuraCastStations
            .Where(s => s.AzuraCast.Guild.UniqueId == guildId && s.StationId == station)
            .SingleOrDefaultAsync();

        if (azuraStation is null)
        {
            _logger.DatabaseAzuraCastStationNotFound(guildId, 0, station);
            return;
        }

        if (stationId.HasValue)
            azuraStation.StationId = stationId.Value;

        if (!string.IsNullOrEmpty(apiKey))
            azuraStation.ApiKey = Crypto.Encrypt(apiKey);

        if (lastSkipTime.HasValue)
            azuraStation.LastSkipTime = DateTimeOffset.UtcNow;

        if (lastRequestTime.HasValue)
            azuraStation.LastRequestTime = DateTimeOffset.UtcNow.AddSeconds(16);

        _dbContext.AzuraCastStations.Update(azuraStation);
    }

    public async Task UpdateAzuraCastStationChecksAsync(ulong guildId, int stationId, bool? fileChanges = null, bool? lastFileChangesCheck = null)
    {
        AzuraCastStationChecksEntity? checks = await _dbContext.AzuraCastStationChecks
            .Where(c => c.Station.AzuraCast.Guild.UniqueId == guildId && c.Station.StationId == stationId)
            .SingleOrDefaultAsync();

        if (checks is null)
        {
            _logger.DatabaseAzuraCastStationChecksNotFound(guildId, 0, stationId);
            return;
        }

        if (fileChanges.HasValue)
            checks.FileChanges = fileChanges.Value;

        if (lastFileChangesCheck.HasValue)
            checks.LastFileChangesCheck = DateTimeOffset.UtcNow;

        _dbContext.AzuraCastStationChecks.Update(checks);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAzuraCastStationPreferencesAsync(ulong guildId, int stationId, ulong? stationAdminGroup = null, ulong? stationDjGroup = null, ulong? fileUploadId = null, ulong? requestId = null, string? fileUploadPath = null, bool? playlist = null)
    {
        AzuraCastStationPreferencesEntity? preferences = await _dbContext.AzuraCastStationPreferences
            .Where(p => p.Station.AzuraCast.Guild.UniqueId == guildId && p.Station.StationId == stationId)
            .SingleOrDefaultAsync();

        if (preferences is null)
        {
            _logger.DatabaseAzuraCastStationPreferencesNotFound(guildId, 0, stationId);
            return;
        }

        if (stationAdminGroup.HasValue)
            preferences.StationAdminRoleId = stationAdminGroup.Value;

        if (stationDjGroup.HasValue)
            preferences.StationDjRoleId = stationDjGroup.Value;

        if (fileUploadId.HasValue)
            preferences.FileUploadChannelId = fileUploadId.Value;

        if (requestId.HasValue)
            preferences.RequestsChannelId = requestId.Value;

        if (!string.IsNullOrEmpty(fileUploadPath))
            preferences.FileUploadPath = fileUploadPath;

        if (playlist.HasValue)
            preferences.ShowPlaylistInNowPlaying = playlist.Value;

        _dbContext.AzuraCastStationPreferences.Update(preferences);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAzzyBotAsync(bool? lastDatabaseCleanup = null, bool? lastUpdateCheck = null)
    {
        AzzyBotEntity? azzyBot = await _dbContext.AzzyBot.SingleOrDefaultAsync();
        if (azzyBot is null)
        {
            _logger.DatabaseAzzyBotNotFound();
            return;
        }

        if (lastDatabaseCleanup.HasValue)
            azzyBot.LastDatabaseCleanup = DateTimeOffset.UtcNow;

        if (lastUpdateCheck.HasValue)
            azzyBot.LastUpdateCheck = DateTimeOffset.UtcNow;

        _dbContext.AzzyBot.Update(azzyBot);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateGuildAsync(ulong guildId, bool? lastPermissionCheck = null, bool? legalsAccepted = null)
    {
        GuildEntity? guild = await _dbContext.Guilds
            .Where(g => g.UniqueId == guildId)
            .SingleOrDefaultAsync();

        if (guild is null)
        {
            _logger.DatabaseGuildNotFound(guildId);
            return;
        }

        if (lastPermissionCheck.HasValue)
            guild.LastPermissionCheck = DateTimeOffset.UtcNow;

        if (legalsAccepted.HasValue)
            guild.LegalsAccepted = legalsAccepted.Value;

        _dbContext.Guilds.Update(guild);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateGuildLegalsAsync()
        => await _dbContext.Guilds.ExecuteUpdateAsync(g => g.SetProperty(p => p.LegalsAccepted, false));

    public async Task UpdateGuildPreferencesAsync(ulong guildId, ulong? adminRoleId = null, ulong? adminNotifiyChannelId = null, ulong? errorChannelId = null)
    {
        GuildPreferencesEntity? preferences = await _dbContext.GuildPreferences
            .Where(p => p.Guild.UniqueId == guildId)
            .Include(static p => p.Guild)
            .SingleOrDefaultAsync();

        if (preferences is null)
        {
            _logger.DatabaseGuildPreferencesNotFound(guildId);
            return;
        }

        if (adminRoleId.HasValue)
            preferences.AdminRoleId = adminRoleId.Value;

        if (adminNotifiyChannelId.HasValue)
            preferences.AdminNotifyChannelId = adminNotifiyChannelId.Value;

        if (errorChannelId.HasValue)
            preferences.ErrorChannelId = errorChannelId.Value;

        if (preferences.AdminRoleId is not 0 && preferences.AdminNotifyChannelId is not 0 && preferences.ErrorChannelId is not 0)
            preferences.Guild.ConfigSet = true;

        _dbContext.GuildPreferences.Update(preferences);
        _dbContext.Guilds.Update(preferences.Guild);
        await _dbContext.SaveChangesAsync();
    }
}
