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

public sealed class AzuraCheckApiPermissionsJob(IAzuraCastApiService apiService, IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly IAzuraCastApiService _apiService = apiService;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            switch (context.Parameter)
            {
                case AzuraCastEntity azura when azura.IsOnline:
                    await _apiService.CheckForApiPermissionsAsync(azura);
                    return;

                case AzuraCastStationEntity station when station.AzuraCast.IsOnline:
                    await _apiService.CheckForApiPermissionsAsync(station);
                    return;
            }

            // Preferences needed to send messages to the guilds channel
            // Stations needed to check the api permissions
            IReadOnlyList<AzuraCastEntity> azuraCasts = await _dbActions.ReadAzuraCastsAsync(loadPrefs: true, loadStations: true);
            if (azuraCasts.Count is 0)
                return;

            IEnumerable<AzuraCastEntity> azuraCastsToCheck = azuraCasts.Where(static a => a.IsOnline);
            await Task.WhenAll(azuraCastsToCheck.Select(_apiService.CheckForApiPermissionsAsync));
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
