using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Data;

public sealed class DbActions(IDbContextFactory<AzzyDbContext> dbContextFactory, ILogger<DbActions> logger)
{
    private readonly IDbContextFactory<AzzyDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<DbActions> _logger = logger;

    private async Task<bool> ExecuteDbActionAsync(Func<AzzyDbContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await action(context);

            await context.SaveChangesAsync();
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
                return;

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
            AzuraCastEntity? azura = await context.Guilds
                .Where(g => g.UniqueId == guildId)
                .OrderBy(g => g.Id)
                .Select(g => g.AzuraCast)
                .FirstOrDefaultAsync();

            if (azura is null)
                return;

            AzuraCastStationEntity station = new()
            {
                StationId = stationId,
                ApiKey = (string.IsNullOrWhiteSpace(apiKey)) ? string.Empty : Crypto.Encrypt(apiKey),
                LastSkipTime = DateTime.MinValue,
                AzuraCastId = azura.Id
            };

            _logger.LogWarning($"CREATING NEW STATION: {station.Id.ToString()}");

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

    public async Task<IReadOnlyList<DiscordGuild>> AddGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        List<GuildEntity> existingGuilds = await context.Guilds
            .OrderBy(g => g.Id)
            .ToListAsync();

        List<GuildEntity> newGuilds = guilds.Keys
            .Where(guild => !existingGuilds.Select(g => g.UniqueId).Contains(guild))
            .Select(guild => new GuildEntity() { UniqueId = guild })
            .ToList();

        if (newGuilds.Count is 0)
            return [];

        bool success = await ExecuteDbActionAsync(async context => await context.Guilds.AddRangeAsync(newGuilds));

        List<DiscordGuild> addedGuilds = newGuilds
            .Where(guild => guilds.ContainsKey(guild.UniqueId))
            .Select(guild => guilds[guild.UniqueId])
            .ToList();

        return (success) ? addedGuilds : [];
    }

    public Task<bool> DeleteAzuraCastAsync(ulong guildId) => ExecuteDbActionAsync(async context => await context.AzuraCast.Where(a => a.Guild.UniqueId == guildId).ExecuteDeleteAsync());
    public Task<bool> DeleteAzuraCastStationAsync(int stationId) => ExecuteDbActionAsync(async context => await context.AzuraCastStations.Where(s => s.StationId == stationId).ExecuteDeleteAsync());

    public Task<bool> DeleteGuildAsync(ulong guildId)
    {
        return ExecuteDbActionAsync(async context =>
        {
            await context.AzuraCast.Where(a => a.Guild.UniqueId == guildId).ExecuteDeleteAsync();
            await context.Guilds.Where(g => g.UniqueId == guildId).ExecuteDeleteAsync();
        });
    }

    public async Task<IReadOnlyList<ulong>> DeleteGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        List<GuildEntity> existingGuilds = await context.Guilds
            .OrderBy(g => g.Id)
            .ToListAsync();

        List<GuildEntity> guildsToDelete = existingGuilds
            .Where(guild => !guilds.Keys.Contains(guild.UniqueId))
            .ToList();

        if (guildsToDelete.Count is 0)
            return [];

        bool success = await ExecuteDbActionAsync(async context =>
        {
            await context.AzuraCast.Where(a => guildsToDelete.Select(g => g.UniqueId).Contains(a.Guild.UniqueId)).ExecuteDeleteAsync();
            await context.Guilds.Where(g => guildsToDelete.Select(g => g.UniqueId).Contains(g.UniqueId)).ExecuteDeleteAsync();
        });

        List<ulong> deletedGuilds = guildsToDelete
            .Where(guild => !guilds.ContainsKey(guild.UniqueId))
            .Select(guld => guld.UniqueId)
            .ToList();

        return (success) ? deletedGuilds : [];
    }

    public async Task<AzuraCastEntity?> GetAzuraCastAsync(ulong guildId, bool loadChecks = false, bool loadPrefs = false, bool loadStations = false, bool loadStationChecks = false, bool loadStationPrefs = false)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        AzuraCastEntity? azuraCast = await context.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .Select(g => g.AzuraCast)
            .FirstOrDefaultAsync();

        if (azuraCast is null)
            return null;

        if (loadChecks)
        {
            await context.Entry(azuraCast)
                .Reference(a => a.Checks)
                .LoadAsync();
        }

        if (loadPrefs)
        {
            await context.Entry(azuraCast)
                .Reference(a => a.Preferences)
                .LoadAsync();
        }

        if (loadStations)
        {
            await context.Entry(azuraCast)
                .Collection(a => a.Stations)
                .LoadAsync();
        }

        if (loadStations && (loadStationChecks || loadStationPrefs))
        {
            foreach (AzuraCastStationEntity station in azuraCast.Stations)
            {
                if (loadStationChecks)
                {
                    await context.Entry(station)
                        .Reference(s => s.Checks)
                        .LoadAsync();
                }

                if (loadStationPrefs)
                {
                    await context.Entry(station)
                        .Reference(s => s.Preferences)
                        .LoadAsync();
                }
            }
        }

        return azuraCast;
    }

    public async Task<AzuraCastPreferencesEntity?> GetAzuraCastPreferencesAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        AzuraCastEntity? azuraCast = await context.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .Select(g => g.AzuraCast)
            .FirstOrDefaultAsync();

        if (azuraCast is null)
            return null;

        await context.Entry(azuraCast)
            .Reference(a => a.Preferences)
            .LoadAsync();

        return azuraCast.Preferences;
    }

    public async Task<AzuraCastStationEntity?> GetAzuraCastStationAsync(ulong guildId, int stationId, bool loadChecks = false, bool loadPrefs = false)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        AzuraCastEntity? azuraCast = await context.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .Select(g => g.AzuraCast)
            .FirstOrDefaultAsync();

        if (azuraCast is null)
            return null;

        await context.Entry(azuraCast)
            .Collection(a => a.Stations)
            .LoadAsync();

        AzuraCastStationEntity? station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId);
        if (station is null)
            return null;

        if (loadChecks)
        {
            await context.Entry(station)
                .Reference(s => s.Checks)
                .LoadAsync();
        }

        if (loadPrefs)
        {
            await context.Entry(station)
                .Reference(s => s.Preferences)
                .LoadAsync();
        }

        return station;
    }

    public async Task<IReadOnlyList<AzuraCastStationEntity>> GetAzuraCastStationsAsync(ulong guildId, bool loadChecks = false, bool loadPrefs = false)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        AzuraCastEntity? azuraCast = await context.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .Select(g => g.AzuraCast)
            .FirstOrDefaultAsync();

        if (azuraCast is null)
            return [];

        await context.Entry(azuraCast)
            .Collection(a => a.Stations)
            .LoadAsync();

        if (loadChecks || loadPrefs)
        {
            foreach (AzuraCastStationEntity station in azuraCast.Stations)
            {
                if (loadChecks)
                {
                    await context.Entry(station)
                        .Reference(s => s.Checks)
                        .LoadAsync();
                }

                if (loadPrefs)
                {
                    await context.Entry(station)
                        .Reference(s => s.Preferences)
                        .LoadAsync();
                }
            }
        }

        return [.. azuraCast.Stations];
    }

    public async Task<AzuraCastStationChecksEntity?> GetAzuraCastStationChecksAsync(ulong guildId, int stationId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        AzuraCastEntity? azuraCast = await context.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .Select(g => g.AzuraCast)
            .FirstOrDefaultAsync();

        if (azuraCast is null)
            return null;

        await context.Entry(azuraCast)
            .Collection(a => a.Stations)
            .LoadAsync();

        return azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId)?.Checks;
    }

    public async Task<AzuraCastStationPreferencesEntity?> GetAzuraCastStationPreferencesAsync(ulong guildId, int stationId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        AzuraCastEntity? azuraCast = await context.Guilds
            .AsNoTracking()
            .Where(g => g.UniqueId == guildId)
            .OrderBy(g => g.Id)
            .Select(g => g.AzuraCast)
            .FirstOrDefaultAsync();

        if (azuraCast is null)
            return null;

        await context.Entry(azuraCast)
            .Collection(a => a.Stations)
            .LoadAsync();

        AzuraCastStationEntity? station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId);
        if (station is null)
            return null;

        await context.Entry(station)
            .Reference(s => s.Preferences)
            .LoadAsync();

        return station.Preferences;
    }

    public async Task<GuildEntity?> GetGuildAsync(ulong guildId, bool loadGuildPrefs = false, bool loadEverything = false)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        GuildEntity? guild = await context.Guilds
            .AsNoTracking()
            .OrderBy(g => g.Id)
            .FirstOrDefaultAsync(g => g.UniqueId == guildId);

        if (guild is null)
            return null;

        if (loadGuildPrefs)
        {
            await context.Entry(guild)
                .Reference(g => g.Preferences)
                .LoadAsync();
        }

        if (loadEverything)
        {
            if (!loadGuildPrefs)
            {
                await context.Entry(guild)
                    .Reference(g => g.Preferences)
                    .LoadAsync();
            }

            await context.Entry(guild)
                .Reference(g => g.AzuraCast)
                .LoadAsync();

            if (guild.AzuraCast is not null)
            {
                await context.Entry(guild.AzuraCast)
                    .Reference(a => a.Checks)
                    .LoadAsync();

                await context.Entry(guild.AzuraCast)
                    .Reference(a => a.Preferences)
                    .LoadAsync();

                await context.Entry(guild.AzuraCast)
                    .Collection(a => a.Stations)
                    .LoadAsync();

                foreach (AzuraCastStationEntity station in guild.AzuraCast.Stations)
                {
                    await context.Entry(station)
                        .Reference(s => s.Checks)
                        .LoadAsync();

                    await context.Entry(station)
                        .Reference(s => s.Preferences)
                        .LoadAsync();
                }
            }
        }

        return guild;
    }

    public async Task<IReadOnlyList<GuildEntity>> GetGuildsAsync(bool loadGuildPrefs = false, bool loadEverything = false)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        List<GuildEntity> guilds = (loadGuildPrefs)
            ? await context.Guilds
                .AsNoTracking()
                .OrderBy(g => g.Id)
                .Include(g => g.Preferences)
                .ToListAsync()
            : await context.Guilds
                .AsNoTracking()
                .OrderBy(g => g.Id)
                .ToListAsync();

        if (loadEverything)
        {
            foreach (GuildEntity guild in guilds)
            {
                if (!loadGuildPrefs)
                {
                    await context.Entry(guild)
                        .Reference(g => g.Preferences)
                        .LoadAsync();
                }

                await context.Entry(guild)
                    .Reference(g => g.AzuraCast)
                    .LoadAsync();

                if (guild.AzuraCast is not null)
                {
                    await context.Entry(guild.AzuraCast)
                        .Reference(a => a.Checks)
                        .LoadAsync();

                    await context.Entry(guild.AzuraCast)
                        .Reference(a => a.Preferences)
                        .LoadAsync();

                    await context.Entry(guild.AzuraCast)
                        .Collection(a => a.Stations)
                        .LoadAsync();

                    foreach (AzuraCastStationEntity station in guild.AzuraCast.Stations)
                    {
                        await context.Entry(station)
                            .Reference(s => s.Checks)
                            .LoadAsync();

                        await context.Entry(station)
                            .Reference(s => s.Preferences)
                            .LoadAsync();
                    }
                }
            }
        }

        return guilds;
    }

    public async Task<GuildPreferencesEntity?> GetGuildPreferencesAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Guilds
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
            AzuraCastEntity? azuraCast = await context.Guilds
                .Where(g => g.UniqueId == guildId)
                .OrderBy(g => g.Id)
                .Select(g => g.AzuraCast)
                .FirstOrDefaultAsync();

            if (azuraCast is null)
                return;

            if (baseUrl is not null)
                azuraCast.BaseUrl = Crypto.Encrypt(baseUrl.OriginalString);

            if (!string.IsNullOrWhiteSpace(apiKey))
                azuraCast.AdminApiKey = Crypto.Encrypt(apiKey);

            if (isOnline.HasValue)
                azuraCast.IsOnline = isOnline.Value;

            context.AzuraCast.Update(azuraCast);
        });
    }

    public Task<bool> UpdateAzuraCastChecksAsync(ulong guildId, bool? serverStatus = null, bool? updates = null, bool? changelog = null, int? updateNotificationCounter = null, DateTime? lastUpdateCheck = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity? azuraCast = await context.Guilds
                .Where(g => g.UniqueId == guildId)
                .OrderBy(g => g.Id)
                .Select(g => g.AzuraCast)
                .FirstOrDefaultAsync();

            if (azuraCast is null)
                return;

            await context.Entry(azuraCast)
                .Reference(a => a.Checks)
                .LoadAsync();

            AzuraCastChecksEntity? checks = azuraCast.Checks;
            if (checks is null)
                return;

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

            context.AzuraCastChecks.Update(checks);
        });
    }

    public Task<bool> UpdateAzuraCastPreferencesAsync(ulong guildId, ulong? instanceAdminGroup = null, ulong? notificationId = null, ulong? outagesId = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity? azuraCast = await context.Guilds
                .Where(g => g.UniqueId == guildId)
                .OrderBy(g => g.Id)
                .Select(g => g.AzuraCast)
                .FirstOrDefaultAsync();

            if (azuraCast is null)
                return;

            await context.Entry(azuraCast)
                .Reference(a => a.Preferences)
                .LoadAsync();

            AzuraCastPreferencesEntity? preferences = azuraCast.Preferences;
            if (preferences is null)
                return;

            if (instanceAdminGroup.HasValue)
                preferences.InstanceAdminRoleId = instanceAdminGroup.Value;

            if (notificationId.HasValue)
                preferences.NotificationChannelId = notificationId.Value;

            if (outagesId.HasValue)
                preferences.OutagesChannelId = outagesId.Value;

            context.AzuraCastPreferences.Update(preferences);
        });
    }

    public Task<bool> UpdateAzuraCastStationAsync(ulong guildId, int station, int? stationId = null, string? apiKey = null, DateTime? lastSkipTime = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity? azuraCast = await context.Guilds
                .Where(g => g.UniqueId == guildId)
                .OrderBy(g => g.Id)
                .Select(g => g.AzuraCast)
                .FirstOrDefaultAsync();

            if (azuraCast is null)
                return;

            await context.Entry(azuraCast)
                .Collection(a => a.Stations)
                .LoadAsync();

            AzuraCastStationEntity? azuraStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (azuraStation is null)
                return;

            if (stationId.HasValue)
                azuraStation.StationId = stationId.Value;

            if (!string.IsNullOrWhiteSpace(apiKey))
                azuraStation.ApiKey = Crypto.Encrypt(apiKey);

            if (lastSkipTime.HasValue)
                azuraStation.LastSkipTime = lastSkipTime.Value;

            context.AzuraCastStations.Update(azuraStation);
        });
    }

    public Task<bool> UpdateAzuraCastStationChecksAsync(ulong guildId, int stationId, bool? fileChanges = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity? azuraCast = await context.Guilds
                .Where(g => g.UniqueId == guildId)
                .OrderBy(g => g.Id)
                .Select(g => g.AzuraCast)
                .FirstOrDefaultAsync();

            if (azuraCast is null)
                return;

            await context.Entry(azuraCast)
                .Collection(a => a.Stations)
                .LoadAsync();

            AzuraCastStationEntity? station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
                return;

            await context.Entry(station)
                .Reference(s => s.Checks)
                .LoadAsync();

            AzuraCastStationChecksEntity? checks = station.Checks;
            if (checks is null)
                return;

            if (fileChanges.HasValue)
                checks.FileChanges = fileChanges.Value;

            context.AzuraCastStationChecks.Update(checks);
        });
    }

    public Task<bool> UpdateAzuraCastStationPreferencesAsync(ulong guildId, int stationId, ulong? stationAdminGroup = null, ulong? stationDjGroup = null, ulong? fileUploadId = null, ulong? requestId = null, string? fileUploadPath = null, bool? playlist = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity? azuraCast = await context.Guilds
                .Where(g => g.UniqueId == guildId)
                .OrderBy(g => g.Id)
                .Select(g => g.AzuraCast)
                .FirstOrDefaultAsync();

            if (azuraCast is null)
                return;

            await context.Entry(azuraCast)
                .Collection(a => a.Stations)
                .LoadAsync();

            AzuraCastStationEntity? station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
                return;

            AzuraCastStationPreferencesEntity? preferences = station.Preferences;
            if (preferences is null)
                return;

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
            GuildEntity? guild = await context.Guilds
                .OrderBy(g => g.Id)
                .FirstOrDefaultAsync(g => g.UniqueId == guildId);

            if (guild is null)
                return;

            await context.Entry(guild)
                .Reference(g => g.Preferences)
                .LoadAsync();

            GuildPreferencesEntity? preferences = guild.Preferences;
            if (preferences is null)
                return;

            if (adminRoleId.HasValue)
                preferences.AdminRoleId = adminRoleId.Value;

            if (adminNotifiyChannelId.HasValue)
                preferences.AdminNotifyChannelId = adminNotifiyChannelId.Value;

            if (errorChannelId.HasValue)
                preferences.ErrorChannelId = errorChannelId.Value;

            if (preferences.AdminRoleId is not 0 && preferences.AdminNotifyChannelId is not 0 && preferences.ErrorChannelId is not 0)
                guild.ConfigSet = true;

            context.GuildPreferences.Update(preferences);
            context.Guilds.Update(guild);
        });
    }
}
