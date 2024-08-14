using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.BackgroundServices;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class TimerServiceHost(ILogger<TimerServiceHost> logger, AzuraChecksBackgroundTask azuraChecksBackgroundService, DbActions dbActions, DiscordBotService discordBotService, UpdaterService updaterService) : IAsyncDisposable, IHostedService
{
    private readonly ILogger<TimerServiceHost> _logger = logger;
    private readonly AzuraChecksBackgroundTask _azuraChecksBackgroundService = azuraChecksBackgroundService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _discordBotService = discordBotService;
    private readonly UpdaterService _updaterService = updaterService;
    private readonly Task _completedTask = Task.CompletedTask;
    private Timer? _timer;
    private DateTime _lastAzzyBotUpdateCheck = DateTime.MinValue;
    private bool _firstRun = true;

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
        if (_firstRun)
            await Task.Delay(TimeSpan.FromSeconds(30));

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
            int guildCount = await _discordBotService.GetDiscordGuildsAsync.CountAsync();
            int delay = 5 + guildCount;

            if (!_firstRun)
            {
                _logger.GlobalTimerCheckForChannelPermissions(guildCount);
                await _discordBotService.CheckPermissionsAsync(guilds);
            }

            await _azuraChecksBackgroundService.QueueInstancePingAsync(guilds, now);

            // Properly wait if there's an exception or not
            await Task.Delay(TimeSpan.FromSeconds(delay));

            await _azuraChecksBackgroundService.QueueApiPermissionChecksAsync(guilds, now);

            // Wait again
            await Task.Delay(TimeSpan.FromSeconds(delay));

            await _azuraChecksBackgroundService.QueueFileChangesChecksAsync(guilds, now);
            await _azuraChecksBackgroundService.QueueUpdatesAsync(guilds, now);

            _firstRun = false;
        }
        catch (Exception ex)
        {
            await _discordBotService.LogExceptionAsync(ex, now);
        }
    }
}
