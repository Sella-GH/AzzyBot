﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

public sealed class TimerServiceHost(ILogger<TimerServiceHost> logger, AzuraCastFileService azuraCastFileService, DiscordBotService discordBotService, UpdaterService updaterService) : IAsyncDisposable, IHostedService
{
    private readonly ILogger<TimerServiceHost> _logger = logger;
    private readonly AzuraCastFileService _azuraCastFileService = azuraCastFileService;
    private readonly DiscordBotService _discordBotService = discordBotService;
    private readonly UpdaterService _updaterService = updaterService;
    private readonly bool _isDev = AzzyStatsSoftware.GetBotEnvironment == Environments.Development;
    private readonly Task _completedTask = Task.CompletedTask;
    private Timer? _timer;
    private DateTime _lastAzuraCastFileCheck = DateTime.MinValue;
    private DateTime _lastBotUpdateCheck = DateTime.MinValue;

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
        if (_timer is IAsyncDisposable timer)
            await timer.DisposeAsync();

        _timer = null;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exception is there to log unkown exceptions")]
    private async void TimerTimeoutAsync(object? o)
    {
        _logger.GlobalTimerTick();

        try
        {
            DateTime now = DateTime.Now;

            if (!_isDev && now - _lastBotUpdateCheck >= TimeSpan.FromHours(5.98))
            {
                _logger.GlobalTimerCheckForUpdates();
                _lastBotUpdateCheck = now;

                await _updaterService.CheckForAzzyUpdatesAsync();
            }

            if (now - _lastAzuraCastFileCheck >= TimeSpan.FromHours(1))
            {
                _logger.GlobalTimerCheckForAzuraCastFiles();
                _lastAzuraCastFileCheck = now;

                _azuraCastFileService.StartAzuraCastFileService();
            }
        }
        catch (Exception ex)
        {
            await _discordBotService.LogExceptionAsync(ex, DateTime.Now);
        }
    }
}
