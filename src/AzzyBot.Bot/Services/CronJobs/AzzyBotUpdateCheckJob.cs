using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using Microsoft.Extensions.Logging;
using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotUpdateCheckJob(ILogger<AzzyBotUpdateCheckJob> logger, UpdaterService updater) : IJob
{
    private readonly ILogger<AzzyBotUpdateCheckJob> _logger = logger;
    private readonly UpdaterService _updater = updater;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        _logger.GlobalTimerCheckForUpdates();

        await _updater.CheckForAzzyUpdatesAsync();
    }
}
