using System.Threading.Tasks;
using System.Threading;
using System;
using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using NCronJob;

namespace AzzyBot.Bot.Services;

public sealed class CronJobManager(IInstantJobRegistry jobRegistry, DiscordBotService botService) : IExceptionHandler
{
    private readonly IInstantJobRegistry _jobRegistry = jobRegistry;
    private readonly DiscordBotService _botService = botService;

    public void RunAzuraRequestJob(AzuraCustomQueueItemRecord record)
        => _jobRegistry.RunInstantJob<AzuraRequestJob>(record);

    public async Task<bool> TryHandleAsync(IJobExecutionContext jobExecutionContext, Exception exception, CancellationToken cancellationToken)
    {
        await _botService.LogExceptionAsync(exception, DateTimeOffset.Now);

        return true;
    }
}
