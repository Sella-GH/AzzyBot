using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class AzzyBackgroundServiceHost(ILogger<AzzyBackgroundServiceHost> logger, IQueuedBackgroundTask taskQueue, DiscordBotService discordBotService) : BackgroundService
{
    private readonly ILogger<AzzyBackgroundServiceHost> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly DiscordBotService _botService = discordBotService;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.BackgroundServiceHostStart();

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.BackgroundServiceHostRun();

        return ProcessQueueAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BackgroundServiceHostStop();

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
            catch (OperationCanceledException)
            {
                _logger.OperationCanceled(nameof(ProcessQueueAsync));
            }
            catch (Exception ex)
            {
                await _botService.LogExceptionAsync(ex, DateTime.Now);
            }
        }
    }
}
