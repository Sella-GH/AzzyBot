using System;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.BackgroundServices;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraChecksBackgroundTask(IHostApplicationLifetime applicationLifetime, ILogger<AzuraChecksBackgroundTask> logger, AzuraCastUpdateService updaterService, QueuedBackgroundTask queue)
{
    private readonly ILogger<AzuraChecksBackgroundTask> _logger = logger;
    private readonly AzuraCastUpdateService _updaterService = updaterService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;
    private readonly QueuedBackgroundTask _queue = queue;

    public void QueueUpdates(GuildEntity guild)
    {
        if (_cancellationToken.IsCancellationRequested)
            return;

        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(guild.AzuraCast);

        _logger.BackgroundServiceWorkItem(nameof(QueueUpdates));

        if (guild.AzuraCast.Checks.Updates)
            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _updaterService.CheckForAzuraCastUpdatesAsync(guild.AzuraCast, ct, true)));
    }
}
