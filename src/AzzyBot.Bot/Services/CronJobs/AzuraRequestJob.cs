using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Logging;
using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraRequestJob(ILogger<AzuraRequestJob> logger, AzuraCastApiService apiService, CronJobManager cronJobManager, DbActions dbActions) : IJob
{
    private readonly ILogger<AzuraRequestJob> _logger = logger;
    private readonly AzuraCastApiService _apiService = apiService;
    private readonly CronJobManager _cronJobManager = cronJobManager;
    private readonly DbActions _dbActions = dbActions;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.BackgroundServiceWorkItem(nameof(RunAsync));

        token.ThrowIfCancellationRequested();

        if (context.Parameter is not AzuraCustomQueueItemRecord record)
            return;

        AzuraCastStationEntity? station = await _dbActions.GetAzuraCastStationAsync(record.GuildId, record.StationId, loadAzuraCast: true);
        if (station is null)
        {
            _logger.DatabaseAzuraCastStationNotFound(record.GuildId, 0, record.StationId);
            return;
        }

        bool requestHasToWait = station.LastRequestTime > record.Timestamp;
        if (requestHasToWait)
        {
            TimeSpan diff = station.LastRequestTime - record.Timestamp;
            _logger.BackgroundServiceSongRequestWaiting(record.RequestId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId, diff.Seconds);
            await Task.Delay(diff, token);
        }

        try
        {
            await _apiService.RequestSongAsync(record.BaseUri, record.StationId, record.RequestId);
            await _dbActions.UpdateAzuraCastStationAsync(record.GuildId, record.StationId, lastRequestTime: true);
            await _dbActions.AddAzuraCastStationRequestAsync(record.GuildId, record.StationId, record.SongId);

            _logger.BackgroundServiceSongRequestFinished(record.RequestId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
        }
        catch (HttpRequestException)
        {
            record.Timestamp = DateTimeOffset.UtcNow;
            _cronJobManager.RunAzuraRequestJob(record);

            _logger.BackgroundServiceSongRequestRequed(record.RequestId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
        }
    }
}
