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

namespace AzzyBot.Bot.Services.BackgroundServices;

public sealed class AzuraChecksBackgroundTask(IHostApplicationLifetime applicationLifetime, ILogger<AzuraChecksBackgroundTask> logger, AzuraCastApiService azuraCastApiService, AzuraCastFileService azuraCastFileService, AzuraCastPingService azuraCastPingService, AzuraCastUpdateService updaterService, QueuedBackgroundTask queue)
{
    private readonly ILogger<AzuraChecksBackgroundTask> _logger = logger;
    private readonly AzuraCastApiService _apiService = azuraCastApiService;
    private readonly AzuraCastFileService _fileService = azuraCastFileService;
    private readonly AzuraCastPingService _pingService = azuraCastPingService;
    private readonly AzuraCastUpdateService _updaterService = updaterService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;
    private readonly QueuedBackgroundTask _queue = queue;

    public async Task QueueApiPermissionChecksAsync(IAsyncEnumerable<GuildEntity> guilds, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        _logger.BackgroundServiceWorkItem(nameof(QueueApiPermissionChecks));

        int counter = 0;
        await foreach (GuildEntity guild in guilds)
        {
            if (guild.AzuraCast?.IsOnline is true && now > guild.AzuraCast.Checks.LastServerStatusCheck.AddMinutes(14.98))
            {
                _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _apiService.CheckForApiPermissionsAsync(guild.AzuraCast)));
                counter++;
            }
        }

        _logger.GlobalTimerCheckForAzuraCastApi(counter);
    }

    public void QueueApiPermissionChecks(GuildEntity guild, int stationId = 0)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(guild.AzuraCast, nameof(guild.AzuraCast));

        _logger.BackgroundServiceWorkItem(nameof(QueueApiPermissionChecks));

        IEnumerable<AzuraCastStationEntity> stations = guild.AzuraCast.Stations;
        if (stationId is not 0)
        {
            AzuraCastStationEntity? station = stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(guild.UniqueId, guild.AzuraCast.Id, stationId);
                return;
            }

            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _apiService.CheckForApiPermissionsAsync(station)));
        }
        else
        {
            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _apiService.CheckForApiPermissionsAsync(guild.AzuraCast)));
        }
    }

    public async Task QueueFileChangesChecksAsync(IAsyncEnumerable<GuildEntity> guilds, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecksAsync));

        int counter = 0;
        await foreach (GuildEntity guild in guilds)
        {
            if (guild.AzuraCast?.IsOnline is true)
            {
                foreach (AzuraCastStationEntity station in guild.AzuraCast!.Stations.Where(s => s.Checks.FileChanges && now > s.Checks.LastFileChangesCheck.AddHours(0.98)))
                {
                    _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _fileService.CheckForFileChangesAsync(station, ct)));
                    counter++;
                }
            }
        }

        _logger.GlobalTimerCheckForAzuraCastFiles(counter);
    }

    public void QueueFileChangesChecks(GuildEntity guild, int stationId = 0)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(guild.AzuraCast, nameof(guild.AzuraCast));

        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecks));

        IEnumerable<AzuraCastStationEntity> stations = guild.AzuraCast.Stations.Where(s => s.Checks.FileChanges);
        if (stationId is not 0)
        {
            AzuraCastStationEntity? station = stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(guild.UniqueId, guild.AzuraCast.Id, stationId);
                return;
            }

            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _fileService.CheckForFileChangesAsync(station, ct)));
        }
        else
        {
            foreach (AzuraCastStationEntity station in stations)
            {
                _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _fileService.CheckForFileChangesAsync(station, ct)));
            }
        }
    }

    public async Task QueueInstancePingAsync(IAsyncEnumerable<GuildEntity> guilds, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        _logger.BackgroundServiceWorkItem(nameof(QueueInstancePingAsync));

        int counter = 0;
        await foreach (GuildEntity guild in guilds)
        {
            if (guild.AzuraCast?.Checks.ServerStatus is true && now > guild.AzuraCast?.Checks.LastServerStatusCheck.AddMinutes(14.98))
            {
                _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _pingService.PingInstanceAsync(guild.AzuraCast, ct)));
                counter++;
            }
        }

        _logger.GlobalTimerCheckForAzuraCastStatus(counter);
    }

    public void QueueInstancePing(GuildEntity guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(guild.AzuraCast, nameof(guild.AzuraCast));

        _logger.BackgroundServiceWorkItem(nameof(QueueInstancePing));

        if (guild.AzuraCast.Checks.ServerStatus)
            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _pingService.PingInstanceAsync(guild.AzuraCast, ct)));
    }

    public async Task QueueUpdatesAsync(IAsyncEnumerable<GuildEntity> guilds, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        _logger.BackgroundServiceWorkItem(nameof(QueueUpdatesAsync));

        int counter = 0;
        await foreach (GuildEntity guild in guilds)
        {
            if (guild.AzuraCast?.IsOnline is true && guild.AzuraCast.Checks.Updates && now > guild.AzuraCast.Checks.LastUpdateCheck.AddHours(11.98))
            {
                _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _updaterService.CheckForAzuraCastUpdatesAsync(guild.AzuraCast!, ct)));
                counter++;
            }
        }

        _logger.GlobalTimerCheckForAzuraCastUpdates(counter);
    }

    public void QueueUpdates(GuildEntity guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(guild.AzuraCast, nameof(guild.AzuraCast));

        _logger.BackgroundServiceWorkItem(nameof(QueueUpdates));

        if (guild.AzuraCast.Checks.Updates)
            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _updaterService.CheckForAzuraCastUpdatesAsync(guild.AzuraCast, ct, true)));
    }
}
