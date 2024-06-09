using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastFileServiceHost(IQueuedBackgroundTask taskQueue, ILogger<AzuraCastFileServiceHost> logger, DiscordBotService discordBotService) : BackgroundService
{
    private readonly ILogger<AzuraCastFileServiceHost> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly DiscordBotService _botService = discordBotService;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.AzuraCastFileServiceHostRun();

        return ProcessQueueAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.AzuraCastFileServiceHostStopped();

        await base.StopAsync(cancellationToken);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We do not know the exception which could be throwing.")]
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Func<CancellationToken, ValueTask>? workItem = await _taskQueue.DequeueAsync(cancellationToken);

                await workItem(cancellationToken);
            }
            catch (Exception ex)
            {
                await _botService.LogExceptionAsync(ex, DateTime.Now);
            }
        }
    }
}
