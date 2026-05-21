using System.Threading.Tasks;

using AzzyBot.Data.Entities;

namespace AzzyBot.Bot.Services.Modules.Interfaces;

public interface IAzuraCastUpdateService
{
    Task CheckForAzuraCastUpdatesAsync(AzuraCastEntity azuraCast, bool forced = false);
}
