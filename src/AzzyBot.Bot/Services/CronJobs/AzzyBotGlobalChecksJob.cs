using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotGlobalChecksJob(ILogger<AzzyBotGlobalChecksJob> logger, AzuraCastFileService azuraFileService, AzuraCastUpdateService azuraUpdateService, DbActions dbActions) : IJob
{
    private readonly ILogger<AzzyBotGlobalChecksJob> _logger = logger;
    private readonly AzuraCastFileService _azuraFileService = azuraFileService;
    private readonly AzuraCastUpdateService _azuraUpdateService = azuraUpdateService;
    private readonly DbActions _dbActions = dbActions;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        _logger.GlobalTimerTick();

        IReadOnlyList<GuildEntity> guilds = await _dbActions.GetGuildsAsync(loadEverything: true);
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        IReadOnlyList<GuildEntity> guildsWorkingSet = [.. guilds.Where(g => g.AzuraCast?.IsOnline is true && g.AzuraCast.Stations.Any(s => s.Checks.FileChanges && utcNow - s.Checks.LastFileChangesCheck >= TimeSpan.FromHours(0.85)))];
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
