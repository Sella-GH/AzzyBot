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

    public async Task<bool> AddAzuraCastAsync(ulong guildId, Uri baseUrl)
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
                GuildId = guild.Id
            };

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

    public async Task<bool> AddAzuraCastStationAsync(ulong guildId, int stationId, string apiKey, ulong requestsId, ulong outagesId, bool hls, bool showPlaylist, bool fileChanges, bool serverStatus, bool updates, bool updatesChangelog)
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
                ApiKey = Crypto.Encrypt(apiKey),
                RequestsChannelId = requestsId,
                OutagesChannelId = outagesId,
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

    public async Task AddBulkGuildsAsync(IReadOnlyList<ulong> guildIds)
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
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity? azuraCast = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azuraCast is null)
                return false;

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
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity? azuraCast = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azuraCast is null)
                return false;

            AzuraCastStationEntity? station = await context.AzuraCastStations.SingleOrDefaultAsync(s => s.AzuraCastId == azuraCast.Id && s.StationId == stationId);
            if (station is null)
                return false;

            AzuraCastMountEntity? mount = await context.AzuraCastMounts.SingleOrDefaultAsync(m => m.StationId == station.Id && m.Id == mountId);
            if (mount is null)
                return false;

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
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity? azuraCast = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azuraCast is null)
                return false;

            AzuraCastStationEntity? station = await context.AzuraCastStations.SingleOrDefaultAsync(s => s.AzuraCastId == azuraCast.Id && s.StationId == stationId);
            if (station is null)
                return false;

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
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

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
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        GuildsEntity guild = await context.Guilds.FirstOrDefaultAsync(g => g.UniqueId == guildId) ?? throw new InvalidOperationException("Guild not found in database");

        return await context.AzuraCast.FirstOrDefaultAsync(a => a.GuildId == guild.Id) ?? throw new InvalidOperationException("AzuraCast settings not found in databse");
    }

    public async Task<AzuraCastChecksEntity> GetAzuraCastChecksAsync(ulong guildId, int stationId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        GuildsEntity guild = await context.Guilds.FirstOrDefaultAsync(g => g.UniqueId == guildId) ?? throw new InvalidOperationException("Guild not found in database");
        AzuraCastEntity azuraCast = await context.AzuraCast.FirstOrDefaultAsync(a => a.GuildId == guild.Id) ?? throw new InvalidOperationException("AzuraCast settings not found in databse");
        AzuraCastStationEntity station = await context.AzuraCastStations.FirstOrDefaultAsync(s => s.AzuraCastId == azuraCast.Id && s.StationId == stationId) ?? throw new InvalidOperationException("Station not found in database");

        return await context.AzuraCastChecks.FirstOrDefaultAsync(c => c.StationId == station.Id) ?? throw new InvalidOperationException("Checks not found in database");
    }

    public async Task<List<AzuraCastMountEntity>> GetAzuraCastMountsAsync(ulong guildId, int stationId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        GuildsEntity guild = await context.Guilds.FirstOrDefaultAsync(g => g.UniqueId == guildId) ?? throw new InvalidOperationException("Guild not found in database");
        AzuraCastEntity azuraCast = await context.AzuraCast.FirstOrDefaultAsync(a => a.GuildId == guild.Id) ?? throw new InvalidOperationException("AzuraCast settings not found in databse");
        AzuraCastStationEntity station = await context.AzuraCastStations.FirstOrDefaultAsync(s => s.AzuraCastId == azuraCast.Id && s.StationId == stationId) ?? throw new InvalidOperationException("Station not found in database");

        return await context.AzuraCastMounts.Where(m => m.StationId == station.Id).ToListAsync();
    }

    public async Task<List<AzuraCastStationEntity>> GetAzuraCastStationsAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        GuildsEntity guild = await context.Guilds.FirstOrDefaultAsync(g => g.UniqueId == guildId) ?? throw new InvalidOperationException("Guild not found in database");
        AzuraCastEntity azuraCast = await context.AzuraCast.FirstOrDefaultAsync(a => a.GuildId == guild.Id) ?? throw new InvalidOperationException("AzuraCast settings not found in databse");

        return await context.AzuraCastStations.Where(s => s.AzuraCastId == azuraCast.Id).ToListAsync();
    }

    public async Task<GuildsEntity> GetGuildAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);

        return guild ?? throw new InvalidOperationException("Guild not found in database.");
    }

    public async Task<List<GuildsEntity>> GetGuildsWithDebugAsync(bool isDebug = true)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();

        return (isDebug)
            ? await context.Guilds.Where(g => g.IsDebugAllowed).ToListAsync()
            : await context.Guilds.Where(g => !g.IsDebugAllowed).ToListAsync();
    }

    public async Task<bool> UpdateAzuraCastAsync(ulong guildId, Uri? baseUrl = null)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity? azuraCast = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azuraCast is null)
                return false;

            if (baseUrl is not null)
                azuraCast.BaseUrl = Crypto.Encrypt(baseUrl.OriginalString);

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
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity? azuraCast = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azuraCast is null)
                return false;

            AzuraCastStationEntity? station = await context.AzuraCastStations.SingleOrDefaultAsync(s => s.AzuraCastId == azuraCast.Id && s.Id == stationId);
            if (station is null)
                return false;

            AzuraCastChecksEntity? checks = await context.AzuraCastChecks.SingleOrDefaultAsync(c => c.StationId == station.Id);
            if (checks is null)
                return false;

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

    public async Task<bool> UpdateAzuraCastStationAsync(ulong guildId, int stationId, string? apiKey, ulong? requestId, ulong? outagesId, bool? hls, bool? playlist)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is null)
                return false;

            AzuraCastEntity? azuraCast = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
            if (azuraCast is null)
                return false;

            AzuraCastStationEntity? station = await context.AzuraCastStations.SingleOrDefaultAsync(s => s.AzuraCastId == azuraCast.Id && s.Id == stationId);
            if (station is null)
                return false;

            if (!string.IsNullOrWhiteSpace(apiKey))
                station.ApiKey = Crypto.Encrypt(apiKey);

            if (requestId.HasValue)
                station.RequestsChannelId = requestId.Value;

            if (outagesId.HasValue)
                station.OutagesChannelId = outagesId.Value;

            if (hls.HasValue)
                station.PreferHls = hls.Value;

            if (playlist.HasValue)
                station.ShowPlaylistInNowPlaying = playlist.Value;

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
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);

            if (guild is null)
                return false;

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
