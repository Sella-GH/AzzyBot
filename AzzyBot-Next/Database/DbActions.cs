using System;
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

    public async Task<bool> AddAzuraCastAsync(ulong guildId, Uri baseUrl, ulong outagesId)
    {
        ArgumentNullException.ThrowIfNull(baseUrl, nameof(baseUrl));

        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity azuraCast = new()
            {
                BaseUrl = Crypto.Encrypt(baseUrl.OriginalString),
                OutagesChannelId = outagesId,
                GuildId = guild.Id
            };

            guild.AzuraCastSet = true;

            await context.AzuraCast.AddAsync(azuraCast);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> AddAzuraCastStationAsync(ulong guildId, int stationId, string name, string apiKey, ulong requestsId, bool hls, bool showPlaylist, bool fileChanges, bool serverStatus, bool updates, bool updatesChangelog)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);

            if (guild is null)
                return false;

            AzuraCastEntity? azura = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azura is null)
                return false;

            AzuraCastStationEntity station = new()
            {
                StationId = stationId,
                Name = Crypto.Encrypt(name),
                ApiKey = Crypto.Encrypt(apiKey),
                RequestsChannelId = requestsId,
                PreferHls = hls,
                ShowPlaylistInNowPlaying = showPlaylist,
                AzuraCastId = azura.Id
            };

            station.Checks = new()
            {
                FileChanges = fileChanges,
                ServerStatus = serverStatus,
                Updates = updates,
                UpdatesShowChangelog = updatesChangelog,
                StationId = station.Id
            };

            await context.AzuraCastStations.AddAsync(station);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> AddAzuraCastMountPointAsync(ulong guildId, int stationId, string mountName, string mount)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity? azura = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azura is null)
                return false;

            AzuraCastStationEntity? station = await context.AzuraCastStations.SingleOrDefaultAsync(s => s.AzuraCastId == azura.Id && s.StationId == stationId);
            if (station is null)
                return false;

            AzuraCastMountEntity mountPoint = new()
            {
                Name = Crypto.Encrypt(mountName),
                Mount = Crypto.Encrypt(mount),
                StationId = station.Id
            };

            await context.AzuraCastMounts.AddAsync(mountPoint);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> AddGuildAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.Guilds.AddAsync(new() { UniqueId = guildId });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task AddGuildsAsync(IReadOnlyList<ulong> guildIds)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            List<ulong> existingGuilds = await context.Guilds.Select(g => g.UniqueId).ToListAsync();
            List<GuildsEntity> newGuilds = guildIds
                .Where(guild => !existingGuilds.Contains(guild))
                .Select(guild => new GuildsEntity() { UniqueId = guild })
                .ToList();

            if (newGuilds.Count > 0)
            {
                await context.Guilds.AddRangeAsync(newGuilds);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }
    }

    public async Task<bool> DeleteAzuraCastAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity guild = await GetGuildAsync(guildId);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast settings not found in database");

            guild.AzuraCastSet = false;

            context.AzuraCast.Remove(azuraCast);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> DeleteAzuraCastMountAsync(ulong guildId, int stationId, int mountId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            AzuraCastMountEntity mount = await GetAzuraCastMountAsync(guildId, stationId, mountId);

            context.AzuraCastMounts.Remove(mount);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> DeleteAzuraCastStationAsync(ulong guildId, int stationId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

            context.AzuraCastStations.Remove(station);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> DeleteGuildAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity guild = await GetGuildAsync(guildId);
            AzuraCastEntity? azuraCast = guild.AzuraCast;
            if (azuraCast is not null)
                context.AzuraCast.Remove(azuraCast);

            context.Guilds.Remove(guild);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<AzuraCastEntity> GetAzuraCastAsync(ulong guildId)
    {
        GuildsEntity guild = await GetGuildAsync(guildId);

        return guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast settings not found in databse");
    }

    public async Task<AzuraCastChecksEntity> GetAzuraCastChecksAsync(ulong guildId, int stationId)
    {
        AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

        return station.Checks;
    }

    public async Task<AzuraCastMountEntity> GetAzuraCastMountAsync(ulong guildId, int stationId, int mountId)
    {
        AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

        return station.Mounts.SingleOrDefault(m => m.Id == mountId) ?? throw new InvalidOperationException("Mount not found in database");
    }

    public async Task<List<AzuraCastMountEntity>> GetAzuraCastMountsAsync(ulong guildId, int stationId)
    {
        AzuraCastStationEntity station = await GetAzuraCastStationAsync(guildId, stationId);

        return [.. station.Mounts];
    }

    public async Task<AzuraCastStationEntity> GetAzuraCastStationAsync(ulong guildId, int stationId)
    {
        AzuraCastEntity azuraCast = await GetAzuraCastAsync(guildId);

        return azuraCast.Stations.SingleOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station not found in database");
    }

    public async Task<List<AzuraCastStationEntity>> GetAzuraCastStationsAsync(ulong guildId)
    {
        AzuraCastEntity azuraCast = await GetAzuraCastAsync(guildId);

        return [.. azuraCast.Stations];
    }

    public async Task<GuildsEntity> GetGuildAsync(ulong guildId)
    {
        List<GuildsEntity> guild = await GetGuildsAsync();

        return guild.SingleOrDefault(g => g.UniqueId == guildId) ?? throw new InvalidOperationException("Guild not found in database");
    }

    public async Task<List<GuildsEntity>> GetGuildsAsync()
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Guilds.ToListAsync();
    }

    public async Task<List<GuildsEntity>> GetGuildsWithDebugAsync(bool isDebug = true)
    {
        List<GuildsEntity> guilds = await GetGuildsAsync();

        return (isDebug)
            ? guilds.Where(g => g.IsDebugAllowed).ToList()
            : guilds.Where(g => !g.IsDebugAllowed).ToList();
    }

    public async Task<bool> UpdateAzuraCastAsync(ulong guildId, Uri? baseUrl, ulong? outagesId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            AzuraCastEntity azuraCast = await GetAzuraCastAsync(guildId);

            if (baseUrl is not null)
                azuraCast.BaseUrl = Crypto.Encrypt(baseUrl.OriginalString);

            if (outagesId.HasValue)
                azuraCast.OutagesChannelId = outagesId.Value;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> UpdateAzuraCastChecksAsync(ulong guildId, int stationId, bool? fileChanges, bool? serverStatus, bool? updates, bool? updatesChangelog)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            AzuraCastChecksEntity checks = await GetAzuraCastChecksAsync(guildId, stationId);

            if (fileChanges.HasValue)
                checks.FileChanges = fileChanges.Value;

            if (serverStatus.HasValue)
                checks.ServerStatus = serverStatus.Value;

            if (updates.HasValue)
                checks.Updates = updates.Value;

            if (updatesChangelog.HasValue)
                checks.UpdatesShowChangelog = updatesChangelog.Value;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> UpdateAzuraCastStationAsync(ulong guildId, int station, int? stationId, string? name, string? apiKey, ulong? requestId, bool? hls, bool? playlist)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            AzuraCastStationEntity azuraStation = await GetAzuraCastStationAsync(guildId, station);

            if (stationId.HasValue)
                azuraStation.StationId = stationId.Value;

            if (!string.IsNullOrWhiteSpace(name))
                azuraStation.Name = Crypto.Encrypt(name);

            if (!string.IsNullOrWhiteSpace(apiKey))
                azuraStation.ApiKey = Crypto.Encrypt(apiKey);

            if (requestId.HasValue)
                azuraStation.RequestsChannelId = requestId.Value;

            if (hls.HasValue)
                azuraStation.PreferHls = hls.Value;

            if (playlist.HasValue)
                azuraStation.ShowPlaylistInNowPlaying = playlist.Value;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }

    public async Task<bool> UpdateGuildAsync(ulong guildId, ulong? errorChannelId, bool? isDebug)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity guild = await GetGuildAsync(guildId);

            if (!guild.ConfigSet)
                guild.ConfigSet = true;

            if (errorChannelId.HasValue)
                guild.ErrorChannelId = errorChannelId.Value;

            if (isDebug.HasValue)
                guild.IsDebugAllowed = isDebug.Value;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        return false;
    }
}
