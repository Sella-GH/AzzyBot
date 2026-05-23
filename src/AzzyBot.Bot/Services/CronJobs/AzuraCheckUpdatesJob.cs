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

public sealed class AzuraCheckUpdatesJob(IAzuraCastUpdateService azuraUpdateService, IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly IAzuraCastUpdateService _azuraUpdateService = azuraUpdateService;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

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
