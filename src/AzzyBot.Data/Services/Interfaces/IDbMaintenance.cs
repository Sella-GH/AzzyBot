using System.Collections.Generic;
using System.Threading.Tasks;

using DSharpPlus.Entities;

namespace AzzyBot.Data.Services.Interfaces;

public interface IDbMaintenance
{
    Task CleanupLeftoverGuildsAsync(IAsyncEnumerable<DiscordGuild> guilds);
}
