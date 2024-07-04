﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Utilities.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Database;

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

            return false;
        }
    }

    public Task<bool> AddAzuraCastAsync(ulong guildId, Uri baseUrl, string apiKey, ulong instanceAdminGroup, ulong notificationId, ulong outagesId, bool serverStatus, bool updates, bool changelog)
    {
        ArgumentNullException.ThrowIfNull(baseUrl, nameof(baseUrl));

        return ExecuteDbActionAsync(async context =>
        {
            GuildsEntity guild = await GetGuildAsync(guildId);

            AzuraCastEntity azuraCast = new()
            {
                BaseUrl = Crypto.Encrypt(baseUrl.OriginalString),
                AdminApiKey = Crypto.Encrypt(apiKey),
                InstanceAdminRoleId = instanceAdminGroup,
                NotificationChannelId = notificationId,
                OutagesChannelId = outagesId,
                GuildId = guild.Id
            };

            azuraCast.Checks = new()
            {
                ServerStatus = serverStatus,
                Updates = updates,
                UpdatesShowChangelog = changelog,
                AzuraCastId = azuraCast.Id
            };

            await context.AzuraCast.AddAsync(azuraCast);
        });
    }

    public Task<bool> AddAzuraCastStationAsync(ulong guildId, int stationId, string name, ulong stationAdminGroup, ulong requestsId, bool hls, bool showPlaylist, bool fileChanges, string? apiKey = null, ulong? stationDjGroup = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity azura = await GetAzuraCastAsync(guildId);

            AzuraCastStationEntity station = new()
            {
                StationId = stationId,
                Name = Crypto.Encrypt(name),
                ApiKey = (string.IsNullOrWhiteSpace(apiKey)) ? string.Empty : Crypto.Encrypt(apiKey),
                StationAdminRoleId = stationAdminGroup,
                StationDjRoleId = stationDjGroup ?? 0,
                RequestsChannelId = requestsId,
                PreferHls = hls,
                ShowPlaylistInNowPlaying = showPlaylist,
                LastSkipTime = DateTime.MinValue,
                AzuraCastId = azura.Id
            };

            station.Checks = new()
            {
                FileChanges = fileChanges,
                StationId = station.Id
            };

            await context.AzuraCastStations.AddAsync(station);
        });
    }

    public Task<bool> AddAzuraCastStationMountPointAsync(ulong guildId, int stationId, string mountName, string mount)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

            AzuraCastStationMountEntity mountPoint = new()
            {
                Name = Crypto.Encrypt(mountName),
                Mount = Crypto.Encrypt(mount),
                StationId = station.Id
            };

            await context.AzuraCastStationMounts.AddAsync(mountPoint);
        });
    }

    public Task<bool> AddGuildAsync(ulong guildId)
        => ExecuteDbActionAsync(async context => await context.Guilds.AddAsync(new() { UniqueId = guildId }));

    public async Task<bool> AddGuildsAsync(IReadOnlyList<ulong> guildIds)
    {
        List<GuildsEntity> guilds = await GetGuildsAsync();
        List<GuildsEntity> newGuilds = guildIds
            .Where(guild => !guilds.Select(g => g.UniqueId).Contains(guild))
            .Select(guild => new GuildsEntity() { UniqueId = guild })
            .ToList();

        if (newGuilds.Count == 0)
            return true;

        return await ExecuteDbActionAsync(async context => await context.Guilds.AddRangeAsync(newGuilds));
    }

    public Task<bool> DeleteAzuraCastAsync(ulong guildId)
    {
        return ExecuteDbActionAsync(async context =>
        {
            GuildsEntity guild = await GetGuildAsync(guildId);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast settings not found in database");

            context.AzuraCast.Remove(azuraCast);
        });
    }

    public Task<bool> DeleteAzuraCastMountAsync(ulong guildId, int stationId, int mountId)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationMountEntity mount = await GetAzuraCastStationMountAsync(guildId, stationId, mountId);

            context.AzuraCastStationMounts.Remove(mount);
        });
    }

    public Task<bool> DeleteAzuraCastStationAsync(ulong guildId, int stationId)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

            context.AzuraCastStations.Remove(station);
        });
    }

    public Task<bool> DeleteGuildAsync(ulong guildId)
    {
        return ExecuteDbActionAsync(async context =>
        {
            GuildsEntity guild = await GetGuildAsync(guildId);
            AzuraCastEntity? azuraCast = guild.AzuraCast;
            if (azuraCast is not null)
                context.AzuraCast.Remove(azuraCast);

            context.Guilds.Remove(guild);
        });
    }

    public async Task<AzuraCastEntity> GetAzuraCastAsync(ulong guildId)
    {
        GuildsEntity guild = await GetGuildAsync(guildId);

        return guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast settings not found in databse");
    }

    public async Task<AzuraCastChecksEntity> GetAzuraCastChecksAsync(ulong guildId)
    {
        AzuraCastEntity azuraCast = await GetAzuraCastAsync(guildId);

        return azuraCast.Checks;
    }

    public async Task<AzuraCastStationEntity> GetAzuraCastStationAsync(ulong guildId, int stationId)
    {
        AzuraCastEntity azuraCast = await GetAzuraCastAsync(guildId);

        return azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station not found in database");
    }

    public async Task<List<AzuraCastStationEntity>> GetAzuraCastStationsAsync(ulong guildId)
    {
        AzuraCastEntity azuraCast = await GetAzuraCastAsync(guildId);

        return [.. azuraCast.Stations];
    }

    public async Task<AzuraCastStationChecksEntity> GetAzuraCastStationChecksAsync(ulong guildId, int stationId)
    {
        AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

        return station.Checks;
    }

    public async Task<AzuraCastStationMountEntity> GetAzuraCastStationMountAsync(ulong guildId, int stationId, int mountId)
    {
        AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

        return station.Mounts.FirstOrDefault(m => m.Id == mountId) ?? throw new InvalidOperationException("Mount not found in database");
    }

    public async Task<List<AzuraCastStationMountEntity>> GetAzuraCastStationMountsAsync(ulong guildId, int stationId)
    {
        AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

        return [.. station.Mounts];
    }

    public async Task<GuildsEntity> GetGuildAsync(ulong guildId)
    {
        List<GuildsEntity> guild = await GetGuildsAsync();

        return guild.Find(g => g.UniqueId == guildId) ?? throw new InvalidOperationException("Guild not found in database");
    }

    public async Task<List<GuildsEntity>> GetGuildsAsync()
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Guilds.AsSplitQuery().ToListAsync();
    }

    public async Task<List<GuildsEntity>> GetGuildsWithDebugAsync(bool isDebug = true)
    {
        List<GuildsEntity> guilds = await GetGuildsAsync();

        return (isDebug)
            ? guilds.Where(g => g.IsDebugAllowed).ToList()
            : guilds.Where(g => !g.IsDebugAllowed).ToList();
    }

    public Task<bool> UpdateAzuraCastAsync(ulong guildId, Uri? baseUrl = null, string? apiKey = null, ulong? instanceAdminGroup = null, ulong? notificationId = null, ulong? outagesId = null, bool? isOnline = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastEntity azuraCast = await GetAzuraCastAsync(guildId);

            if (baseUrl is not null)
                azuraCast.BaseUrl = Crypto.Encrypt(baseUrl.OriginalString);

            if (!string.IsNullOrWhiteSpace(apiKey))
                azuraCast.AdminApiKey = Crypto.Encrypt(apiKey);

            if (instanceAdminGroup.HasValue)
                azuraCast.InstanceAdminRoleId = instanceAdminGroup.Value;

            if (notificationId.HasValue)
                azuraCast.NotificationChannelId = notificationId.Value;

            if (outagesId.HasValue)
                azuraCast.OutagesChannelId = outagesId.Value;

            if (isOnline.HasValue)
                azuraCast.IsOnline = isOnline.Value;

            context.AzuraCast.Update(azuraCast);
        });
    }

    public Task<bool> UpdateAzuraCastChecksAsync(ulong guildId, bool? serverStatus = null, bool? updates = null, bool? changelog = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastChecksEntity checks = await GetAzuraCastChecksAsync(guildId);

            if (serverStatus.HasValue)
                checks.ServerStatus = serverStatus.Value;

            if (updates.HasValue)
                checks.Updates = updates.Value;

            if (changelog.HasValue)
                checks.UpdatesShowChangelog = changelog.Value;

            context.AzuraCastChecks.Update(checks);
        });
    }

    public Task<bool> UpdateAzuraCastStationAsync(ulong guildId, int station, int? stationId = null, string? name = null, string? apiKey = null, ulong? stationAdminGroup = null, ulong? stationDjGroup = null, ulong? requestId = null, bool? hls = null, bool? playlist = null, DateTime? lastSkipTime = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationEntity azuraStation = await GetAzuraCastStationAsync(guildId, station);

            if (stationId.HasValue)
                azuraStation.StationId = stationId.Value;

            if (!string.IsNullOrWhiteSpace(name))
                azuraStation.Name = Crypto.Encrypt(name);

            if (!string.IsNullOrWhiteSpace(apiKey))
                azuraStation.ApiKey = Crypto.Encrypt(apiKey);

            if (stationAdminGroup.HasValue)
                azuraStation.StationAdminRoleId = stationAdminGroup.Value;

            if (stationDjGroup.HasValue)
                azuraStation.StationDjRoleId = stationDjGroup.Value;

            if (requestId.HasValue)
                azuraStation.RequestsChannelId = requestId.Value;

            if (hls.HasValue)
                azuraStation.PreferHls = hls.Value;

            if (playlist.HasValue)
                azuraStation.ShowPlaylistInNowPlaying = playlist.Value;

            if (lastSkipTime.HasValue)
                azuraStation.LastSkipTime = lastSkipTime.Value;

            context.AzuraCastStations.Update(azuraStation);
        });
    }

    public Task<bool> UpdateAzuraCastStationChecksAsync(ulong guildId, int stationId, bool? fileChanges = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            AzuraCastStationChecksEntity checks = await GetAzuraCastStationChecksAsync(guildId, stationId);

            if (fileChanges.HasValue)
                checks.FileChanges = fileChanges.Value;

            context.AzuraCastStationChecks.Update(checks);
        });
    }

    public Task<bool> UpdateGuildAsync(ulong guildId, ulong? adminRoleId = null, ulong? adminNotifiyChannelId = null, ulong? errorChannelId = null, bool? isDebug = null)
    {
        return ExecuteDbActionAsync(async context =>
        {
            GuildsEntity guild = await GetGuildAsync(guildId);

            if (!guild.ConfigSet)
                guild.ConfigSet = true;

            if (adminRoleId.HasValue)
                guild.AdminRoleId = adminRoleId.Value;

            if (adminNotifiyChannelId.HasValue)
                guild.AdminNotifyChannelId = adminNotifiyChannelId.Value;

            if (errorChannelId.HasValue)
                guild.ErrorChannelId = errorChannelId.Value;

            if (isDebug.HasValue)
                guild.IsDebugAllowed = isDebug.Value;

            context.Guilds.Update(guild);
        });
    }
}
