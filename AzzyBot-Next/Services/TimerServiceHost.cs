using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

public sealed class TimerServiceHost(AzzyBotSettingsRecord settings, DiscordBotService discordBotService, UpdaterService updaterService, ILogger<TimerServiceHost> logger) : IDisposable, IHostedService
{
    private readonly ILogger<TimerServiceHost> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DiscordBotService _discordBotService = discordBotService;
    private readonly UpdaterService _updaterService = updaterService;
    private readonly bool _isDev = AzzyStatsSoftware.GetBotEnvironment == Environments.Development;
    private Timer? _timer;
    private DateTime _lastBotUpdateCheck = DateTime.MinValue;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.GlobalTimerStart();
        _timer = new(new TimerCallback(TimerTimeoutAsync), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logger.GlobalTimerStop();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exception is there to log unkown exceptions")]
    private async void TimerTimeoutAsync(object? o)
    {
        _logger.GlobalTimerTicked();

        try
        {
            if (!_isDev)
            {
                DateTime now = DateTime.Now;

                if (now - _lastBotUpdateCheck >= TimeSpan.FromDays(_settings.Updater.CheckInterval))
                {
                    _logger.GlobalTimerCheckForUpdates();
                    _lastBotUpdateCheck = now;

                    await _updaterService.CheckForAzzyUpdatesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            await _discordBotService.LogExceptionAsync(ex, DateTime.Now);
        }
    }
}
