using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Data.Entities;

using DSharpPlus.Entities;

using NCronJob;

namespace AzzyBot.Bot.Services.Interfaces;

public interface ICronJobManager : IExceptionHandler
{
    void RunAzuraCheckApiPermissionsJob(AzuraCastEntity azuraCast);
    void RunAzuraCheckApiPermissionsJob(AzuraCastStationEntity station);
    void RunAzuraCheckFileChangesJob(AzuraCastEntity azuraCast);
    void RunAzuraCheckFileChangesJob(AzuraCastStationEntity station);
    void RunAzuraCheckUpdatesJob(AzuraCastEntity azuraCast);
    void RunAzuraRequestJob(AzuraCustomQueueItemModel queueItem);
    void RunAzuraStatusPingJob(AzuraCastEntity azuraCast);
    void RunAzzyBotCheckPermissionsJob(DiscordGuild guild, ulong[] guildIds);
    void RunAzzyBotCheckPermissionsJob(GuildEntity guild);
    void RunAzzyBotInactiveGuildJob();
}
