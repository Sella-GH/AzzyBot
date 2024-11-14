using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.BackgroundServices;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraChecksBackgroundTask(IHostApplicationLifetime applicationLifetime, ILogger<AzuraChecksBackgroundTask> logger, AzuraCastPingService azuraCastPingService, AzuraCastUpdateService updaterService, QueuedBackgroundTask queue)
{
    private readonly ILogger<AzuraChecksBackgroundTask> _logger = logger;
    private readonly AzuraCastPingService _pingService = azuraCastPingService;
    private readonly AzuraCastUpdateService _updaterService = updaterService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;
    private readonly QueuedBackgroundTask _queue = queue;

    public void QueueInstancePing(GuildEntity guild)
    {
        if (_cancellationToken.IsCancellationRequested)
            return;

        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(guild.AzuraCast);

        _logger.BackgroundServiceWorkItem(nameof(QueueInstancePing));

        if (guild.AzuraCast.Checks.ServerStatus)
            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _pingService.PingInstanceAsync(guild.AzuraCast, ct)));
    }

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
