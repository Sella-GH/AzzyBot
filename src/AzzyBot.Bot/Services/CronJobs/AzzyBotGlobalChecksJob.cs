using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
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
    private DateTimeOffset _lastAzzyUpdateCheck = DateTimeOffset.MinValue;
    private DateTimeOffset _lastDatabaseCleanup = DateTimeOffset.MinValue;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        _logger.GlobalTimerTick();
        IReadOnlyList<GuildEntity> guilds = await _dbActions.GetGuildsAsync(loadEverything: true);
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        if (utcNow - _lastDatabaseCleanup >= TimeSpan.FromHours(24))
        {
            await _dbMaintenance.CleanupLeftoverGuildsAsync(_discordClient.Guilds);
            _lastDatabaseCleanup = utcNow;
        }

        if (utcNow - _lastAzzyUpdateCheck >= TimeSpan.FromHours(6))
        {
            _logger.GlobalTimerCheckForUpdates();
            await _updater.CheckForAzzyUpdatesAsync();
            _lastAzzyUpdateCheck = utcNow;
        }

        List<GuildEntity> guildsWorkingSet = guilds.Where(g => utcNow - g.LastPermissionCheck >= TimeSpan.FromHours(12)).ToList();
        _logger.GlobalTimerCheckForChannelPermissions(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
            await _botService.CheckPermissionsAsync(guildsWorkingSet);

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.Checks.ServerStatus is true && utcNow - g.AzuraCast.Checks.LastServerStatusCheck >= TimeSpan.FromMinutes(15)).ToList();
        _logger.GlobalTimerCheckForAzuraCastStatus(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
        {
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraPingService.PingInstanceAsync(guild.AzuraCast!);
            }
        }

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.IsOnline is true && utcNow - g.AzuraCast.Checks.LastServerStatusCheck >= TimeSpan.FromHours(12)).ToList();
        _logger.GlobalTimerCheckForAzuraCastApi(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
        {
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraApiService.CheckForApiPermissionsAsync(guild.AzuraCast!);
            }
        }

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.IsOnline is true && g.AzuraCast.Stations.Any(s => s.Checks.FileChanges)).ToList();
        _logger.GlobalTimerCheckForAzuraCastFiles(guildsWorkingSet.Count);
        if (guildsWorkingSet.Count is not 0)
        {
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                foreach (AzuraCastStationEntity station in guild.AzuraCast!.Stations.Where(s => s.Checks.FileChanges && utcNow - s.Checks.LastFileChangesCheck >= TimeSpan.FromHours(1)))
                {
                    await _azuraFileService.CheckForFileChangesAsync(station);
                }
            }
        }

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.IsOnline is true && g.AzuraCast.Checks.Updates && utcNow - g.AzuraCast.Checks.LastUpdateCheck >= TimeSpan.FromHours(6)).ToList();
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
