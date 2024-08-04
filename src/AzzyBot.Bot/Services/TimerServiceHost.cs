using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.BackgroundServices;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class TimerServiceHost(ILogger<TimerServiceHost> logger, AzuraChecksBackgroundTask azuraCastBackgroundService, DbActions dbActions, DiscordBotService discordBotService, UpdaterService updaterService) : IAsyncDisposable, IHostedService
{
    private readonly ILogger<TimerServiceHost> _logger = logger;
    private readonly AzuraChecksBackgroundTask _azuraCastBackgroundService = azuraCastBackgroundService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _discordBotService = discordBotService;
    private readonly UpdaterService _updaterService = updaterService;
    private readonly Task _completedTask = Task.CompletedTask;
    private Timer? _timer;
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

        DateTime now = DateTime.Now;
        try
        {
            if (now - _lastAzzyBotUpdateCheck >= TimeSpan.FromHours(5.98))
            {
                _logger.GlobalTimerCheckForUpdates();
                _lastAzzyBotUpdateCheck = now;

                await _updaterService.CheckForAzzyUpdatesAsync();
            }

            IAsyncEnumerable<GuildEntity> guilds = _dbActions.GetGuildsAsync(loadEverything: true);
            int delay = 5 + _discordBotService.GetDiscordGuilds.Count;

            await _azuraCastBackgroundService.StartBackgroundServiceAsync(AzuraCastChecks.CheckForOnlineStatus, guilds);

            // Properly wait if there's an exception or not
            await Task.Delay(TimeSpan.FromSeconds(delay));

            await _azuraCastBackgroundService.StartBackgroundServiceAsync(AzuraCastChecks.CheckForApiPermissions, guilds);

            // Wait again
            await Task.Delay(TimeSpan.FromSeconds(delay));

            await _azuraCastBackgroundService.StartBackgroundServiceAsync(AzuraCastChecks.CheckForFileChanges, guilds);
            await _azuraCastBackgroundService.StartBackgroundServiceAsync(AzuraCastChecks.CheckForUpdates, guilds);
        }
        catch (Exception ex)
        {
            await _discordBotService.LogExceptionAsync(ex, now);
        }
    }
}
