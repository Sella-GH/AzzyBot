using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraCheckFileChangesJob(AzuraCastFileService azuraFileService, DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly AzuraCastFileService _azuraFileService = azuraFileService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<AzuraCastEntity> azuraCasts = await _dbActions.ReadAzuraCastsAsync(loadPrefs: true, loadStations: true, loadStationChecks: true, loadGuild: true);
            if (!azuraCasts.Any())
                return;

            foreach (AzuraCastEntity azuraCast in azuraCasts)
            {
                foreach (AzuraCastStationEntity station in azuraCast.Stations.Where(s => s.Checks.FileChanges))
                {
                    await _azuraFileService.CheckForFileChangesAsync(station);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
