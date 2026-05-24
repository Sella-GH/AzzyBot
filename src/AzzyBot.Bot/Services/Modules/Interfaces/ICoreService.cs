using System.Collections.Generic;
using System.Threading.Tasks;

using AzzyBot.Bot.Structs;
using AzzyBot.Data.Entities;

namespace AzzyBot.Bot.Services.Modules.Interfaces;

public interface ICoreService
{
    Task<IReadOnlyDictionary<GuildEntity, AzzyInactiveGuildStruct>> CheckUnusedGuildsAsync();
    Task<int> DeleteUnusedGuildsAsync(IReadOnlyDictionary<GuildEntity, AzzyInactiveGuildStruct> guilds);
    Task<int> NotifyUnusedGuildsAsync(IReadOnlyDictionary<GuildEntity, AzzyInactiveGuildStruct> guilds);
}
