using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Logging;
using AzzyBot.Data.Services.Interfaces;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraRequestJob(ILogger<AzuraRequestJob> logger, IAzuraCastApiService apiService, ICronJobManager cronJobManager, IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly ILogger<AzuraRequestJob> _logger = logger;
    private readonly IAzuraCastApiService _apiService = apiService;
    private readonly ICronJobManager _cronJobManager = cronJobManager;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            _logger.BackgroundServiceWorkItem(nameof(RunAsync));

            token.ThrowIfCancellationRequested();

            if (context.Parameter is not AzuraCustomQueueItemModel queueItem)
                return;

            AzuraCastStationEntity? station = await _dbActions.ReadAzuraCastStationAsync(queueItem.GuildId, queueItem.StationId, loadAzuraCast: true);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(queueItem.GuildId, 0, queueItem.StationId);
                return;
            }

            bool requestHasToWait = station.LastRequestTime > queueItem.Timestamp;
            if (requestHasToWait)
            {
                TimeSpan diff = station.LastRequestTime - queueItem.Timestamp;
                _logger.BackgroundServiceSongRequestWaiting(queueItem.RequestId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId, diff.Seconds);
                await Task.Delay(diff, token);
            }

            try
            {
                await _apiService.RequestSongAsync(queueItem.BaseUri, queueItem.StationId, queueItem.RequestId);
                await _dbActions.UpdateAzuraCastStationAsync(queueItem.GuildId, queueItem.StationId, updateLastRequestTime: true);
                await _dbActions.CreateAzuraCastStationRequestAsync(queueItem.GuildId, queueItem.StationId, queueItem.SongId);

                _logger.BackgroundServiceSongRequestFinished(queueItem.RequestId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
            }
            catch (HttpRequestException)
            {
                queueItem.Timestamp = DateTimeOffset.UtcNow;
                _cronJobManager.RunAzuraRequestJob(queueItem);

                _logger.BackgroundServiceSongRequestRequeued(queueItem.RequestId, station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
