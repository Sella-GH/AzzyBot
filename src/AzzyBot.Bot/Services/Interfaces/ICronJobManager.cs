using AzzyBot.Bot.Models.AzuraCast;

using NCronJob;

namespace AzzyBot.Bot.Services.Interfaces;

public interface ICronJobManager : IExceptionHandler
{
    void RunAzuraCheckApiPermissionsJob();
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
