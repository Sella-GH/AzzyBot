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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Data;

public sealed class DbActions(ILogger<DbActions> logger, AzzyDbContext dbContext)
{
    private readonly ILogger<DbActions> _logger = logger;
    private readonly AzzyDbContext _dbContext = dbContext;

    private async Task<bool> ExecuteDbActionAsync(Func<AzzyDbContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            await action(_dbContext);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();

            throw;
        }
    }

    public Task<bool> AddAzuraCastAsync(ulong guildId, Uri baseUrl, string apiKey, ulong instanceAdminGroup, ulong notificationId, ulong outagesId, bool serverStatus, bool updates, bool changelog)
    {
        ArgumentNullException.ThrowIfNull(baseUrl, nameof(baseUrl));

        return ExecuteDbActionAsync(async context =>
        {
            GuildEntity? guild = await context.Guilds
                .OrderBy(g => g.Id)
                .FirstOrDefaultAsync(g => g.UniqueId == guildId);

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

            await context.AzuraCast.AddAsync(azuraCast);
        });
    }

    public Task<bool> AddAzuraCastStationAsync(ulong guildId, int stationId, ulong stationAdminGroup, ulong requestsId, bool showPlaylist, bool fileChanges, ulong? fileUploadId = null, string? fileUploadPath = null, string? apiKey = null, ulong? stationDjGroup = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity? azura = await context.AzuraCast
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync(a => a.Guild.UniqueId == guildId);

            if (azura is null)
            {
                _logger.DatabaseAzuraCastNotFound(guildId);
                return;
            }

            AzuraCastStationEntity station = new()
            {
                StationId = stationId,
                ApiKey = (string.IsNullOrWhiteSpace(apiKey)) ? string.Empty : Crypto.Encrypt(apiKey),
                LastSkipTime = DateTime.MinValue,
                LastRequestTime = DateTime.MinValue,
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

            await context.AzuraCastStations.AddAsync(station);
        });
    }

    public Task<bool> AddGuildAsync(ulong guildId)
        => ExecuteDbActionAsync(async context => await context.Guilds.AddAsync(new() { UniqueId = guildId }));

    public async Task<IEnumerable<DiscordGuild>> AddGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        IEnumerable<GuildEntity> existingGuilds = _dbContext.Guilds
            .OrderBy(g => g.Id);

        IEnumerable<GuildEntity> newGuilds = guilds.Keys
            .Where(guild => !existingGuilds.Select(g => g.UniqueId).Contains(guild))
            .Select(guild => new GuildEntity() { UniqueId = guild })
            .ToList();

        if (!newGuilds.Any())
            return [];

        bool success = await ExecuteDbActionAsync(async context => await context.Guilds.AddRangeAsync(newGuilds));

        IEnumerable<DiscordGuild> addedGuilds = newGuilds
            .Where(guild => guilds.ContainsKey(guild.UniqueId))
            .Select(guild => guilds[guild.UniqueId]);

        return (success) ? addedGuilds : [];
    }

    public Task<bool> DeleteAzuraCastAsync(ulong guildId)
        => ExecuteDbActionAsync(async context => await context.AzuraCast.Where(a => a.Guild.UniqueId == guildId).ExecuteDeleteAsync());

    public Task<bool> DeleteAzuraCastStationAsync(int stationId)
        => ExecuteDbActionAsync(async context => await context.AzuraCastStations.Where(s => s.StationId == stationId).ExecuteDeleteAsync());

    public Task<bool> DeleteGuildAsync(ulong guildId)
    {
        return ExecuteDbActionAsync(async context =>
        {
            await context.AzuraCast.Where(a => a.Guild.UniqueId == guildId).ExecuteDeleteAsync();
            await context.Guilds.Where(g => g.UniqueId == guildId).ExecuteDeleteAsync();
        });
    }

    public async Task<IEnumerable<ulong>> DeleteGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        IEnumerable<GuildEntity> existingGuilds = _dbContext.Guilds
            .OrderBy(g => g.Id);

        IEnumerable<GuildEntity> guildsToDelete = existingGuilds
            .Where(guild => !guilds.Keys.Contains(guild.UniqueId))
            .ToList();

        if (!guildsToDelete.Any())
            return [];

        bool success = await ExecuteDbActionAsync(async context =>
        {
            await context.AzuraCast.Where(a => guildsToDelete.Select(g => g.UniqueId).Contains(a.Guild.UniqueId)).ExecuteDeleteAsync();
            await context.Guilds.Where(g => guildsToDelete.Select(g => g.UniqueId).Contains(g.UniqueId)).ExecuteDeleteAsync();
        });

        IEnumerable<ulong> deletedGuilds = guildsToDelete
            .Where(guild => !guilds.ContainsKey(guild.UniqueId))
            .Select(guld => guld.UniqueId);

        return (success) ? deletedGuilds : [];
    }

    public Task<AzuraCastEntity?> GetAzuraCastAsync(ulong guildId, bool loadChecks = false, bool loadPrefs = false, bool loadStations = false, bool loadStationChecks = false, bool loadStationPrefs = false, bool loadGuild = false)
    {
        return _dbContext.AzuraCast
            .AsNoTracking()
            .Where(a => a.Guild.UniqueId == guildId)
            .OrderBy(a => a.Id)
            .IncludeIf(loadChecks, q => q.Include(a => a.Checks))
            .IncludeIf(loadPrefs, q => q.Include(a => a.Preferences))
            .IncludeIf(loadStations, q => q.Include(a => a.Stations))
            .IncludeIf(loadStations && loadStationChecks, q => q.Include(a => a.Stations).ThenInclude(s => s.Checks))
            .IncludeIf(loadStations && loadStationPrefs, q => q.Include(a => a.Stations).ThenInclude(s => s.Preferences))
            .IncludeIf(loadGuild, q => q.Include(a => a.Guild))
            .FirstOrDefaultAsync();
    }

    public Task<AzuraCastPreferencesEntity?> GetAzuraCastPreferencesAsync(ulong guildId, bool loadAzuraCast = false)
    {
        return _dbContext.AzuraCastPreferences
            .AsNoTracking()
            .Where(p => p.AzuraCast.Guild.UniqueId == guildId)
            .OrderBy(p => p.Id)
            .IncludeIf(loadAzuraCast, q => q.Include(p => p.AzuraCast))
            .FirstOrDefaultAsync();
    }

    public Task<AzuraCastStationEntity?> GetAzuraCastStationAsync(ulong guildId, int stationId, bool loadChecks = false, bool loadPrefs = false, bool loadAzuraCast = false)
    {
        return _dbContext.AzuraCastStations
            .AsNoTracking()
            .Where(s => s.AzuraCast.Guild.UniqueId == guildId && s.StationId == stationId)
            .OrderBy(s => s.Id)
            .IncludeIf(loadChecks, q => q.Include(s => s.Checks))
            .IncludeIf(loadPrefs, q => q.Include(s => s.Preferences))
            .IncludeIf(loadAzuraCast, q => q.Include(s => s.AzuraCast))
            .FirstOrDefaultAsync();
    }

    public IAsyncEnumerable<AzuraCastStationEntity> GetAzuraCastStationsAsync(ulong guildId, bool loadChecks = false, bool loadPrefs = false, bool loadAzuraCast = false)
    {
        return _dbContext.AzuraCastStations
            .AsNoTracking()
            .Where(s => s.AzuraCast.Guild.UniqueId == guildId)
            .OrderBy(s => s.Id)
            .IncludeIf(loadChecks, q => q.Include(s => s.Checks))
            .IncludeIf(loadPrefs, q => q.Include(s => s.Preferences))
            .IncludeIf(loadAzuraCast, q => q.Include(s => s.AzuraCast))
            .AsAsyncEnumerable();
    }

    public Task<AzuraCastStationChecksEntity?> GetAzuraCastStationChecksAsync(ulong guildId, int stationId, bool loadStation = false)
    {
        return _dbContext.AzuraCastStationChecks
            .AsNoTracking()
            .Where(c => c.Station.AzuraCast.Guild.UniqueId == guildId && c.Station.StationId == stationId)
            .OrderBy(c => c.Id)
            .IncludeIf(loadStation, q => q.Include(c => c.Station))
            .FirstOrDefaultAsync();
    }

    public Task<AzuraCastStationPreferencesEntity?> GetAzuraCastStationPreferencesAsync(ulong guildId, int stationId, bool loadStation = false)
    {
        return _dbContext.AzuraCastStationPreferences
            .AsNoTracking()
            .Where(p => p.Station.AzuraCast.Guild.UniqueId == guildId && p.Station.StationId == stationId)
            .OrderBy(p => p.Id)
            .IncludeIf(loadStation, q => q.Include(p => p.Station))
            .FirstOrDefaultAsync();
    }

    public IAsyncEnumerable<GuildEntity> GetGuildAsync(ulong guildId, bool loadGuildPrefs = false, bool loadEverything = false)
    {
        return _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .IncludeIf(loadGuildPrefs || loadEverything, q => q.Include(g => g.Preferences))
            .IncludeIf(loadEverything, q => q.Include(g => g.AzuraCast).Include(g => g.AzuraCast!.Checks).Include(g => g.AzuraCast!.Preferences))
            .IncludeIf(loadEverything, q => q.Include(g => g.AzuraCast!.Stations).ThenInclude(s => s.Checks))
            .IncludeIf(loadEverything, q => q.Include(g => g.AzuraCast!.Stations).ThenInclude(s => s.Preferences))
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<GuildEntity> GetGuildsAsync(bool loadGuildPrefs = false, bool loadEverything = false)
    {
        return _dbContext.Guilds
            .AsNoTracking()
            .OrderBy(g => g.Id)
            .IncludeIf(loadGuildPrefs || loadEverything, q => q.Include(g => g.Preferences))
            .IncludeIf(loadEverything, q => q.Include(g => g.AzuraCast).Include(g => g.AzuraCast!.Checks).Include(g => g.AzuraCast!.Preferences))
            .IncludeIf(loadEverything, q => q.Include(g => g.AzuraCast!.Stations).ThenInclude(s => s.Checks))
            .IncludeIf(loadEverything, q => q.Include(g => g.AzuraCast!.Stations).ThenInclude(s => s.Preferences))
            .AsAsyncEnumerable();
    }

    public Task<GuildPreferencesEntity?> GetGuildPreferencesAsync(ulong guildId)
    {
        return _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .Select(g => g.Preferences)
            .FirstOrDefaultAsync();
    }

    public Task<bool> UpdateAzuraCastAsync(ulong guildId, Uri? baseUrl = null, string? apiKey = null, bool? isOnline = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity? azuraCast = await context.AzuraCast
                .Where(a => a.Guild.UniqueId == guildId)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync();

            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(guildId);
                return;
            }

            if (baseUrl is not null)
                azuraCast.BaseUrl = Crypto.Encrypt(baseUrl.OriginalString);

            if (!string.IsNullOrWhiteSpace(apiKey))
                azuraCast.AdminApiKey = Crypto.Encrypt(apiKey);

            if (isOnline.HasValue)
                azuraCast.IsOnline = isOnline.Value;

            context.AzuraCast.Update(azuraCast);
        });
    }

    public Task<bool> UpdateAzuraCastChecksAsync(ulong guildId, bool? serverStatus = null, bool? updates = null, bool? changelog = null, int? updateNotificationCounter = null, DateTime? lastUpdateCheck = null, DateTime? lastServerStatusCheck = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastChecksEntity? checks = await context.AzuraCastChecks
                .Where(c => c.AzuraCast.Guild.UniqueId == guildId)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

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
                checks.LastUpdateCheck = lastUpdateCheck.Value;

            if (lastServerStatusCheck.HasValue)
                checks.LastServerStatusCheck = lastServerStatusCheck.Value;

            context.AzuraCastChecks.Update(checks);
        });
    }

    public Task<bool> UpdateAzuraCastPreferencesAsync(ulong guildId, ulong? instanceAdminGroup = null, ulong? notificationId = null, ulong? outagesId = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastPreferencesEntity? preferences = await context.AzuraCastPreferences
                .Where(p => p.AzuraCast.Guild.UniqueId == guildId)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

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

            context.AzuraCastPreferences.Update(preferences);
        });
    }

    public Task<bool> UpdateAzuraCastStationAsync(ulong guildId, int station, int? stationId = null, string? apiKey = null, DateTime? lastSkipTime = null, DateTime? lastRequestTime = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationEntity? azuraStation = await context.AzuraCastStations
                .Where(s => s.AzuraCast.Guild.UniqueId == guildId && s.StationId == station)
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();

            if (azuraStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(guildId, 0, station);
                return;
            }

            if (stationId.HasValue)
                azuraStation.StationId = stationId.Value;

            if (!string.IsNullOrWhiteSpace(apiKey))
                azuraStation.ApiKey = Crypto.Encrypt(apiKey);

            if (lastSkipTime.HasValue)
                azuraStation.LastSkipTime = lastSkipTime.Value;

            if (lastRequestTime.HasValue)
                azuraStation.LastRequestTime = lastRequestTime.Value;

            context.AzuraCastStations.Update(azuraStation);
        });
    }

    public Task<bool> UpdateAzuraCastStationChecksAsync(ulong guildId, int stationId, bool? fileChanges = null, DateTime? lastFileChangesCheck = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationChecksEntity? checks = await context.AzuraCastStationChecks
                .Where(c => c.Station.AzuraCast.Guild.UniqueId == guildId && c.Station.StationId == stationId)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (checks is null)
            {
                _logger.DatabaseAzuraCastStationChecksNotFound(guildId, 0, stationId);
                return;
            }

            if (fileChanges.HasValue)
                checks.FileChanges = fileChanges.Value;

            if (lastFileChangesCheck.HasValue)
                checks.LastFileChangesCheck = lastFileChangesCheck.Value;

            context.AzuraCastStationChecks.Update(checks);
        });
    }

    public Task<bool> UpdateAzuraCastStationPreferencesAsync(ulong guildId, int stationId, ulong? stationAdminGroup = null, ulong? stationDjGroup = null, ulong? fileUploadId = null, ulong? requestId = null, string? fileUploadPath = null, bool? playlist = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationPreferencesEntity? preferences = await context.AzuraCastStationPreferences
                .Where(p => p.Station.AzuraCast.Guild.UniqueId == guildId && p.Station.StationId == stationId)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

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

            if (!string.IsNullOrWhiteSpace(fileUploadPath))
                preferences.FileUploadPath = fileUploadPath;

            if (playlist.HasValue)
                preferences.ShowPlaylistInNowPlaying = playlist.Value;

            context.AzuraCastStationPreferences.Update(preferences);
        });
    }

    public Task<bool> UpdateGuildPreferencesAsync(ulong guildId, ulong? adminRoleId = null, ulong? adminNotifiyChannelId = null, ulong? errorChannelId = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            GuildPreferencesEntity? preferences = await context.GuildPreferences
                .Where(p => p.Guild.UniqueId == guildId)
                .Include(p => p.Guild)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

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

            context.GuildPreferences.Update(preferences);
            context.Guilds.Update(preferences.Guild);
        });
    }
}
