using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using NCronJob;

namespace AzzyBot.Bot.Services;

public sealed class CronJobManager(IInstantJobRegistry jobRegistry)
{
    private readonly IInstantJobRegistry _jobRegistry = jobRegistry;

    public void RunAzuraRequestJob(AzuraCustomQueueItemRecord record)
        => _jobRegistry.RunInstantJob<AzuraRequestJob>(record);
}
