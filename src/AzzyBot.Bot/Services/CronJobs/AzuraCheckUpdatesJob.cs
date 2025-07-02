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

public sealed class AzuraCheckUpdatesJob(AzuraCastUpdateService azuraUpdateService, DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly AzuraCastUpdateService _azuraUpdateService = azuraUpdateService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<AzuraCastEntity> azuraCasts = await _dbActions.ReadAzuraCastsAsync(loadChecks: true, loadPrefs: true, loadGuild: true);
            if (!azuraCasts.Any())
                return;

            foreach (AzuraCastEntity azuraCast in azuraCasts.Where(a => a.IsOnline && a.Checks.Updates))
            {
                await _azuraUpdateService.CheckForAzuraCastUpdatesAsync(azuraCast);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
