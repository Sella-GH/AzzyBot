using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzzyBot.Bot.Services.Interfaces;

public interface IUpdaterService
{
    IReadOnlyDictionary<string, string> GitHubHeaders { get; }
    Task CheckForAzzyUpdatesAsync();
}
