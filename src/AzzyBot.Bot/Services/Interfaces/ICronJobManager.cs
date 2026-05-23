using AzzyBot.Bot.Utilities.Records.AzuraCast;

using NCronJob;

namespace AzzyBot.Bot.Services.Interfaces;

public interface ICronJobManager : IExceptionHandler
{
    void RunAzzyBotInactiveGuildJob();
    void RunAzuraRequestJob(AzuraCustomQueueItemRecord record);
}
