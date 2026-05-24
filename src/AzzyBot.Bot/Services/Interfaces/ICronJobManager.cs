using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Data.Entities;

using NCronJob;

namespace AzzyBot.Bot.Services.Interfaces;

public interface ICronJobManager : IExceptionHandler
{
    void RunAzuraCheckApiPermissionsJob(AzuraCastEntity azuraCast);
    void RunAzuraCheckApiPermissionsJob(AzuraCastStationEntity station);
    void RunAzuraCheckFileChangesJob();
    void RunAzuraCheckUpdatesJob();
    void RunAzuraPersistentNowPlayingJob();
    void RunAzuraRequestJob(AzuraCustomQueueItemModel queueItem);
    void RunAzuraStatusPingJob();
    void RunAzzyBotCheckPermissionsJob();
    void RunAzzyBotInactiveGuildJob();
    void RunAzzyBotUpdateCheckJob();
    void RunDatabaseCleaningJob();
    void RunLogfileCleaningJob();
    void RunMusicStreamingPersistentNowPlayingJob();
}
