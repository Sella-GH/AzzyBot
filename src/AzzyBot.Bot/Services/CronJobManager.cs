using System;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Services.Interfaces;

using NCronJob;

namespace AzzyBot.Bot.Services;

public sealed class CronJobManager(IInstantJobRegistry jobRegistry, IDiscordBotService botService) : ICronJobManager
{
    private readonly IInstantJobRegistry _jobRegistry = jobRegistry;
    private readonly IDiscordBotService _botService = botService;

    public void RunAzzyBotInactiveGuildJob()
        => _jobRegistry.RunInstantJob<AzzyBotInactiveGuildJob>();

    public void RunAzuraRequestJob(AzuraCustomQueueItemModel queueItem)
        => _jobRegistry.RunInstantJob<AzuraRequestJob>(queueItem);

    public async Task<bool> TryHandleAsync(IJobExecutionContext jobExecutionContext, Exception exception, CancellationToken cancellationToken)
    {
        await _botService.LogExceptionAsync(exception, DateTimeOffset.Now);

        return true;
    }
}
