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

public sealed class AzuraStatusPingJob(IAzuraCastPingService pingService, IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly IAzuraCastPingService _pingService = pingService;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            if (context.Parameter is not AzuraCastEntity azuraCast)
            {
                IReadOnlyList<AzuraCastEntity> azuraCasts = await _dbActions.ReadAzuraCastsAsync(loadChecks: true, loadPrefs: true, loadGuild: true);
                if (azuraCasts.Count is 0)
                    return;

                IEnumerable<AzuraCastEntity> azuraCastsToPing = azuraCasts.Where(static a => a.Checks.ServerStatus);
                await Task.WhenAll(azuraCastsToPing.Select(_pingService.PingInstanceAsync));

                return;
            }

            await _pingService.PingInstanceAsync(azuraCast);
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
