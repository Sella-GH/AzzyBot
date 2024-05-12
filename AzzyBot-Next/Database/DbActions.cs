using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Database;

internal sealed class DbActions(IDbContextFactory<AzzyDbContext> dbContextFactory, ILogger<DbActions> logger)
{
    private readonly IDbContextFactory<AzzyDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<DbActions> _logger = logger;

    internal async Task AddGuildEntityAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.Guilds.AddAsync(new() { UniqueId = guildId, AzuraCast = new() { AutomaticChecks = new() } });
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }
    }

    internal async Task AddBulkGuildEntitiesAsync(List<ulong> guildIds)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            List<ulong> existingGuilds = await context.Guilds.Select(g => g.UniqueId).ToListAsync();
            List<GuildsEntity> newGuilds = guildIds
                .Where(guild => !existingGuilds.Contains(guild))
                .Select(guild => new GuildsEntity() { UniqueId = guild, AzuraCast = new() { AutomaticChecks = new() } })
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

    internal async Task RemoveGuildEntityAsync(ulong guildId)
    {
        await using AzzyDbContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity? guild = await context.Guilds.SingleOrDefaultAsync(g => g.UniqueId == guildId);
            if (guild is not null)
            {
                AzuraCastEntity? azura = await context.AzuraCast.SingleOrDefaultAsync(a => a.GuildId == guild.Id);
                if (azura is not null)
                {
                    AzuraCastChecksEntity? checks = await context.AzuraCastChecks.SingleOrDefaultAsync(c => c.AzuraCastId == azura.Id);
                    if (checks is not null)
                        context.AzuraCastChecks.Remove(checks);

                    context.AzuraCast.Remove(azura);
                }

                context.Guilds.Remove(guild);
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
}
