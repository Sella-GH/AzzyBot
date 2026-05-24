using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraCheckFileChangesJob(IAzuraCastFileService azuraFileService, IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly IAzuraCastFileService _azuraFileService = azuraFileService;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            IEnumerable<AzuraCastStationEntity> stationsToCheck;

            switch (context.Parameter)
            {
                case AzuraCastEntity azuraCast:
                    stationsToCheck = azuraCast.Stations.Where(static s => s.Checks.FileChanges);
                    await Task.WhenAll(stationsToCheck.Select(_azuraFileService.CheckForFileChangesAsync));
                    return;

                case AzuraCastStationEntity azuraCastStation:
                    await _azuraFileService.CheckForFileChangesAsync(azuraCastStation);
                    return;
            }

            IReadOnlyList<AzuraCastEntity> azuraCasts = await _dbActions.ReadAzuraCastsAsync(loadPrefs: true, loadStations: true, loadStationChecks: true, loadGuild: true);
            if (azuraCasts.Count is 0)
                return;

            stationsToCheck = azuraCasts.SelectMany(static ac => ac.Stations.Where(static s => s.Checks.FileChanges));
            await Task.WhenAll(stationsToCheck.Select(_azuraFileService.CheckForFileChangesAsync));
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
