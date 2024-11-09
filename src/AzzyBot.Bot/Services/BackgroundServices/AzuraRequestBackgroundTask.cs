using System;
using System.Net.Http;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.BackgroundServices;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.BackgroundServices;

public sealed class AzuraRequestBackgroundTask(ILogger<AzuraRequestBackgroundTask> logger, AzuraCastApiService azuraCastApiService, DbActions dbActions, QueuedBackgroundTask queue)
{
    private readonly ILogger<AzuraRequestBackgroundTask> _logger = logger;
    private readonly AzuraCastApiService _apiService = azuraCastApiService;
    private readonly DbActions _dbActions = dbActions;
    private readonly QueuedBackgroundTask _queue = queue;

    public async Task CreateRequestAsync(AzuraCustomQueueItemRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        _logger.BackgroundServiceWorkItem(nameof(CreateRequestAsync));

        AzuraCastStationEntity? station = await _dbActions.GetAzuraCastStationAsync(record.GuildId, record.StationId, loadAzuraCast: true);
        if (station is null)
        {
            _logger.DatabaseAzuraCastStationNotFound(record.GuildId, 0, record.StationId);
            return;
        }

        bool isLarger = station.LastRequestTime > record.Timestamp;
        if (isLarger)
        {
            TimeSpan diff = station.LastRequestTime - record.Timestamp;
            _logger.BackgroundServiceSongRequestWaiting(record.SongId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId, diff.Seconds);
            await Task.Delay(diff);

            record.Timestamp = DateTimeOffset.UtcNow;
            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await CreateRequestAsync(record)));
            _logger.BackgroundServiceSongRequestRequed(record.SongId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);

            return;
        }

        try
        {
            await _apiService.RequestSongAsync(record.BaseUri, record.StationId, record.SongId);
            await _dbActions.UpdateAzuraCastStationAsync(record.GuildId, record.StationId, lastRequestTime: DateTimeOffset.UtcNow.AddSeconds(16));

            _logger.BackgroundServiceSongRequestFinished(record.SongId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
        }
        catch (HttpRequestException)
        {
            record.Timestamp = DateTimeOffset.UtcNow;
            _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await CreateRequestAsync(record)));

            _logger.BackgroundServiceSongRequestRequed(record.SongId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
        }
    }
}
