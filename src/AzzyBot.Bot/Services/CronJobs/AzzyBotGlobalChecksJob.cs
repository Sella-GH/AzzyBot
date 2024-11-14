using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Logging;
using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotGlobalChecksJob(ILogger<AzzyBotGlobalChecksJob> logger, AzuraCastApiService azuraApiService, AzuraCastFileService azuraFileService, AzuraCastPingService azuraPingService, AzuraCastUpdateService azuraUpdateService, DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly ILogger<AzzyBotGlobalChecksJob> _logger = logger;
    private readonly AzuraCastApiService _azuraApiService = azuraApiService;
    private readonly AzuraCastFileService _azuraFileService = azuraFileService;
    private readonly AzuraCastPingService _azuraPingService = azuraPingService;
    private readonly AzuraCastUpdateService _azuraUpdateService = azuraUpdateService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        _logger.GlobalTimerTick();
        List<GuildEntity> guilds = await _dbActions.GetGuildsAsync(loadEverything: true).ToListAsync(token);

        _logger.GlobalTimerCheckForChannelPermissions();
        List<GuildEntity> guildsWorkingSet = guilds.Where(static g => DateTimeOffset.UtcNow - g.LastPermissionCheck >= TimeSpan.FromHours(12)).ToList();
        if (guildsWorkingSet.Count is not 0)
            await _botService.CheckPermissionsAsync(guildsWorkingSet);

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.Checks.ServerStatus is true && DateTimeOffset.UtcNow - g.AzuraCast.Checks.LastServerStatusCheck >= TimeSpan.FromMinutes(15)).ToList();
        if (guildsWorkingSet.Count is not 0)
        {
            _logger.GlobalTimerCheckForAzuraCastStatus(guildsWorkingSet.Count);
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraPingService.PingInstanceAsync(guild.AzuraCast!, token);
            }
        }

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.IsOnline is true && DateTimeOffset.UtcNow - g.AzuraCast.Checks.LastServerStatusCheck >= TimeSpan.FromHours(12)).ToList();
        if (guildsWorkingSet.Count is not 0)
        {
            _logger.GlobalTimerCheckForAzuraCastApi(guildsWorkingSet.Count);
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraApiService.CheckForApiPermissionsAsync(guild.AzuraCast!);
            }
        }

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.IsOnline is true && g.AzuraCast.Stations.Any(s => s.Checks.FileChanges)).ToList();
        if (guildsWorkingSet.Count is not 0)
        {
            _logger.GlobalTimerCheckForAzuraCastFiles(guildsWorkingSet.Count);

            foreach (GuildEntity guild in guildsWorkingSet)
            {
                foreach (AzuraCastStationEntity station in guild.AzuraCast!.Stations.Where(s => s.Checks.FileChanges && DateTimeOffset.UtcNow - s.Checks.LastFileChangesCheck >= TimeSpan.FromHours(1)))
                {
                    await _azuraFileService.CheckForFileChangesAsync(station, token);
                }
            }
        }

        guildsWorkingSet = guilds.Where(g => g.AzuraCast?.IsOnline is true && g.AzuraCast.Checks.Updates && DateTimeOffset.UtcNow - g.AzuraCast.Checks.LastUpdateCheck >= TimeSpan.FromHours(6)).ToList();
        if (guildsWorkingSet.Count is not 0)
        {
            _logger.GlobalTimerCheckForAzuraCastUpdates(guildsWorkingSet.Count);
            foreach (GuildEntity guild in guildsWorkingSet)
            {
                await _azuraUpdateService.CheckForAzuraCastUpdatesAsync(guild.AzuraCast!, token);
            }
        }
    }
}
