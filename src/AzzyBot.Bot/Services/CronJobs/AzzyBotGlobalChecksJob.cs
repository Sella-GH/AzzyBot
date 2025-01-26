using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotGlobalChecksJob(ILogger<AzzyBotGlobalChecksJob> logger, AzuraCastApiService azuraApiService, AzuraCastFileService azuraFileService, AzuraCastPingService azuraPingService, AzuraCastUpdateService azuraUpdateService, DbActions dbActions, DbMaintenance dbMaintenance, DiscordBotService botService, DiscordClient discordClient, UpdaterService updater) : IJob
{
    private readonly ILogger<AzzyBotGlobalChecksJob> _logger = logger;
    private readonly AzuraCastApiService _azuraApiService = azuraApiService;
    private readonly AzuraCastFileService _azuraFileService = azuraFileService;
    private readonly AzuraCastPingService _azuraPingService = azuraPingService;
    private readonly AzuraCastUpdateService _azuraUpdateService = azuraUpdateService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DbMaintenance _dbMaintenance = dbMaintenance;
    private readonly DiscordBotService _botService = botService;
    private readonly DiscordClient _discordClient = discordClient;
    private readonly UpdaterService _updater = updater;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        _logger.GlobalTimerTick();

        AzzyBotEntity azzyBot = await _dbActions.GetAzzyBotAsync() ?? throw new InvalidOperationException("AzzyBot entity is missing from the database.");
        IReadOnlyList<GuildEntity> guilds = await _dbActions.GetGuildsAsync(loadEverything: true);
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        if (utcNow - azzyBot.LastDatabaseCleanup >= TimeSpan.FromHours(23.85))
            await _dbMaintenance.CleanupLeftoverGuildsAsync(_discordClient.Guilds);

        if (utcNow - azzyBot.LastUpdateCheck >= TimeSpan.FromHours(5.85))
            await _updater.CheckForAzzyUpdatesAsync();

        List<GuildEntity> guildsWorkingSet = [.. guilds.Where(g => utcNow - g.LastPermissionCheck >= TimeSpan.FromHours(11.85))];
        _logger.GlobalTimerCheckForChannelPermissions(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
            await _botService.CheckPermissionsAsync(guildsWorkingSet);

        guildsWorkingSet = [.. guilds.Where(g => g.AzuraCast?.Checks.ServerStatus is true)];
        _logger.GlobalTimerCheckForAzuraCastStatus(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
        {
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraPingService.PingInstanceAsync(guild.AzuraCast!);
            }
        }

        // Get it just one more time to check if the instance is offline
        guilds = await _dbActions.GetGuildsAsync(loadEverything: true);
        guildsWorkingSet = [.. guilds.Where(g => g.AzuraCast?.IsOnline is true && utcNow - g.AzuraCast.Checks.LastServerStatusCheck >= TimeSpan.FromHours(11.85))];
        _logger.GlobalTimerCheckForAzuraCastApi(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
        {
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraApiService.CheckForApiPermissionsAsync(guild.AzuraCast!);
            }
        }

        guildsWorkingSet = [.. guilds.Where(g => g.AzuraCast?.IsOnline is true && g.AzuraCast.Stations.Any(s => s.Checks.FileChanges && utcNow - s.Checks.LastFileChangesCheck >= TimeSpan.FromHours(0.85)))];
        _logger.GlobalTimerCheckForAzuraCastFiles(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
        {
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                foreach (AzuraCastStationEntity station in guild.AzuraCast!.Stations.Where(s => s.Checks.FileChanges && utcNow - s.Checks.LastFileChangesCheck >= TimeSpan.FromHours(0.85)))
                {
                    await _azuraFileService.CheckForFileChangesAsync(station);
                }
            }
        }

        guildsWorkingSet = [.. guilds.Where(g => g.AzuraCast?.IsOnline is true && g.AzuraCast.Checks.Updates && utcNow - g.AzuraCast.Checks.LastUpdateCheck >= TimeSpan.FromHours(5.85))];
        _logger.GlobalTimerCheckForAzuraCastUpdates(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
        {
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraUpdateService.CheckForAzuraCastUpdatesAsync(guild.AzuraCast!);
            }
        }
    }
}
