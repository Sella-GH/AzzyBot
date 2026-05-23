using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Data.Logging;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Data.Services;

public sealed class DbMaintenance(ILogger<DbMaintenance> logger, IDbActions dbActions) : IDbMaintenance
{
    private readonly ILogger<DbMaintenance> _logger = logger;
    private readonly IDbActions _dbActions = dbActions;

    public async Task CleanupLeftoverGuildsAsync(IAsyncEnumerable<DiscordGuild> guilds)
    {
        _logger.DatabaseOrphanedGuildsStart();

        IEnumerable<ulong> removedGuilds = await _dbActions.DeleteGuildsAsync(guilds);
        await _dbActions.UpdateAzzyBotAsync(updateLastDatabaseCleanup: true);

        _logger.DatabaseOrphanedGuildsComplete(removedGuilds.Count());
    }
}
