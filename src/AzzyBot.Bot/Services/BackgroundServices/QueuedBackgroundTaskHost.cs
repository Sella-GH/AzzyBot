using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.BackgroundServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.BackgroundServices;

public sealed class QueuedBackgroundTaskHost(ILogger<QueuedBackgroundTaskHost> logger, QueuedBackgroundTask taskQueue, DiscordBotService discordBotService) : BackgroundService
{
    private readonly ILogger<QueuedBackgroundTaskHost> _logger = logger;
    private readonly QueuedBackgroundTask _taskQueue = taskQueue;
    private readonly DiscordBotService _botService = discordBotService;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.BackgroundServiceHostStart();

        await base.StartAsync(cancellationToken);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We do not know the exception which could be throwing.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.BackgroundServiceHostRun();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Func<CancellationToken, ValueTask>? workItem = await _taskQueue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.OperationCanceled(nameof(ExecuteAsync));
            }
            catch (Exception ex)
            {
                await _botService.LogExceptionAsync(ex, DateTime.Now);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BackgroundServiceHostStop();

        await base.StopAsync(cancellationToken);
    }
}
