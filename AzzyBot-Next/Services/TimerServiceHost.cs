using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

public sealed class TimerServiceHost(ILogger<TimerServiceHost> logger, AzzyBackgroundService azuraCastBackgroundService, DiscordBotService discordBotService, UpdaterService updaterService) : IAsyncDisposable, IHostedService
{
    private readonly ILogger<TimerServiceHost> _logger = logger;
    private readonly AzzyBackgroundService _azuraCastBackgroundService = azuraCastBackgroundService;
    private readonly DiscordBotService _discordBotService = discordBotService;
    private readonly UpdaterService _updaterService = updaterService;
    private readonly bool _isDev = AzzyStatsSoftware.GetBotEnvironment == Environments.Development;
    private readonly Task _completedTask = Task.CompletedTask;
    private Timer? _timer;
    private DateTime _lastAzuraCastFileCheck = DateTime.MinValue;
    private DateTime _lastAzuraCastUpdateCheck = DateTime.MinValue;
    private DateTime _lastAzzyBotUpdateCheck = DateTime.MinValue;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.GlobalTimerStart();
        _timer = new(new TimerCallback(TimerTimeoutAsync), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));

        return _completedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logger.GlobalTimerStop();

        return _completedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is not null)
            await _timer.DisposeAsync();

        _timer = null;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exception is there to log unkown exceptions")]
    private async void TimerTimeoutAsync(object? o)
    {
        _logger.GlobalTimerTick();

        try
        {
            DateTime now = DateTime.Now;

            if (!_isDev && now - _lastAzzyBotUpdateCheck >= TimeSpan.FromHours(5.98))
            {
                _logger.GlobalTimerCheckForUpdates();
                _lastAzzyBotUpdateCheck = now;

                await _updaterService.CheckForAzzyUpdatesAsync();
            }

            if (now - _lastAzuraCastFileCheck >= TimeSpan.FromHours(0.98))
            {
                _logger.GlobalTimerCheckForAzuraCastFiles();
                _lastAzuraCastFileCheck = now;

                await _azuraCastBackgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForFileChanges);
            }

            if (now - _lastAzuraCastUpdateCheck >= TimeSpan.FromHours(11.98))
            {
                _logger.GlobalTimerCheckForAzuraCastUpdates();
                _lastAzuraCastUpdateCheck = now;

                await _azuraCastBackgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForUpdates);
            }
        }
        catch (Exception ex)
        {
            await _discordBotService.LogExceptionAsync(ex, DateTime.Now);
        }
    }
}
