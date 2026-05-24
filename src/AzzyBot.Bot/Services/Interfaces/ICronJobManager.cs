using AzzyBot.Bot.Models.AzuraCast;

using NCronJob;

namespace AzzyBot.Bot.Services.Interfaces;

public interface ICronJobManager : IExceptionHandler
{
    void RunAzzyBotInactiveGuildJob();
    void RunAzuraRequestJob(AzuraCustomQueueItemModel queueItem);
}
