using System;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Interfaces;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotUpdateCheckJob(DiscordBotService botService, IUpdaterService updater) : IJob
{
    private readonly DiscordBotService _botService = botService;
    private readonly IUpdaterService _updater = updater;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            await _updater.CheckForAzzyUpdatesAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
