using System;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Data.Entities;

using NCronJob;

namespace AzzyBot.Bot.Services;

public sealed class CronJobManager(IInstantJobRegistry jobRegistry, IDiscordBotService botService) : ICronJobManager
{
    private readonly IInstantJobRegistry _jobRegistry = jobRegistry;
    private readonly IDiscordBotService _botService = botService;

    public void RunAzuraCheckApiPermissionsJob(AzuraCastEntity azuraCast)
        => _jobRegistry.RunInstantJob<AzuraCheckApiPermissionsJob>(azuraCast);

    public void RunAzuraCheckApiPermissionsJob(AzuraCastStationEntity station)
        => _jobRegistry.RunInstantJob<AzuraCheckApiPermissionsJob>(station);

    public void RunAzuraCheckFileChangesJob()
        => _jobRegistry.RunInstantJob<AzuraCheckFileChangesJob>();

    public void RunAzuraCheckUpdatesJob()
        => _jobRegistry.RunInstantJob<AzuraCheckUpdatesJob>();

    public void RunAzuraPersistentNowPlayingJob()
        => _jobRegistry.RunInstantJob<AzuraPersistentNowPlayingJob>();

    public void RunAzuraRequestJob(AzuraCustomQueueItemModel queueItem)
        => _jobRegistry.RunInstantJob<AzuraRequestJob>(queueItem);

    public void RunAzuraStatusPingJob()
        => _jobRegistry.RunInstantJob<AzuraStatusPingJob>();

    public void RunAzzyBotCheckPermissionsJob()
        => _jobRegistry.RunInstantJob<AzzyBotCheckPermissionsJob>();

    public void RunAzzyBotInactiveGuildJob()
        => _jobRegistry.RunInstantJob<AzzyBotInactiveGuildJob>();

    public void RunAzzyBotUpdateCheckJob()
        => _jobRegistry.RunInstantJob<AzzyBotUpdateCheckJob>();

    public void RunDatabaseCleaningJob()
        => _jobRegistry.RunInstantJob<DatabaseCleaningJob>();

    public void RunLogfileCleaningJob()
        => _jobRegistry.RunInstantJob<LogfileCleaningJob>();

    public void RunMusicStreamingPersistentNowPlayingJob()
        => _jobRegistry.RunInstantJob<MusicStreamingPersistentNowPlayingJob>();

    public async Task<bool> TryHandleAsync(IJobExecutionContext jobExecutionContext, Exception exception, CancellationToken cancellationToken)
    {
        await _botService.LogExceptionAsync(exception, DateTimeOffset.Now);

        return true;
    }
}
