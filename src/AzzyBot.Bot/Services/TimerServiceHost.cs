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

public sealed class TimerServiceHost(ILogger<TimerServiceHost> logger, AzuraChecksBackgroundTask azuraChecksBackgroundService, DbActions dbActions, DbMaintenance dbMaintenance, DiscordBotService discordBotService, UpdaterService updaterService) : IAsyncDisposable, IHostedService
{
    private readonly ILogger<TimerServiceHost> _logger = logger;
    private readonly AzuraChecksBackgroundTask _azuraChecksBackgroundService = azuraChecksBackgroundService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DbMaintenance _dbMaintenance = dbMaintenance;
    private readonly DiscordBotService _discordBotService = discordBotService;
    private readonly UpdaterService _updaterService = updaterService;
    private readonly Task _completedTask = Task.CompletedTask;
    private DateTimeOffset _lastAzzyBotUpdateCheck = DateTimeOffset.MinValue;
    private DateTimeOffset _lastCleanup = DateTimeOffset.MinValue;
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.GlobalTimerStart();
        _timer = new(new TimerCallback(TimerTimeoutAsync), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(15));

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

        DateTimeOffset now = DateTimeOffset.UtcNow;
        try
        {
            if (now - _lastCleanup > TimeSpan.FromHours(23.98))
            {
                await _dbMaintenance.CleanupLeftoverGuildsAsync(_discordBotService.GetDiscordGuilds);
                _lastCleanup = now;
            }

            if (now - _lastAzzyBotUpdateCheck > TimeSpan.FromHours(5.98))
            {
                _logger.GlobalTimerCheckForUpdates();
                await _updaterService.CheckForAzzyUpdatesAsync();
                _lastAzzyBotUpdateCheck = now;
            }

            IAsyncEnumerable<GuildEntity> guilds = _dbActions.GetGuildsAsync(loadEverything: true);
            int delay = 5 + await guilds.CountAsync();

            _logger.GlobalTimerCheckForChannelPermissions();
            await _discordBotService.CheckPermissionsAsync(guilds);

            await _azuraChecksBackgroundService.QueueInstancePingAsync(guilds);

            // Properly wait if there's an exception or not
            await Task.Delay(TimeSpan.FromSeconds(delay));

            await _azuraChecksBackgroundService.QueueApiPermissionChecksAsync(guilds);

            // Wait again
            await Task.Delay(TimeSpan.FromSeconds(delay));

            await _azuraChecksBackgroundService.QueueFileChangesChecksAsync(guilds);
            await _azuraChecksBackgroundService.QueueUpdatesAsync(guilds);
        }
        catch (Exception ex)
        {
            await _discordBotService.LogExceptionAsync(ex, now);
        }
    }
}
