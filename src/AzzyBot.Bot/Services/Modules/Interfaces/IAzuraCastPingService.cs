using System.Threading.Tasks;

using AzzyBot.Data.Entities;

namespace AzzyBot.Bot.Services.Modules.Interfaces;

public interface IAzuraCastPingService
{
    Task PingInstanceAsync(AzuraCastEntity azuraCast);
}
