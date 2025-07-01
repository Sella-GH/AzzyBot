using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Core.Logging;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Data.Services;

public sealed class DbMaintenance(ILogger<DbMaintenance> logger, DbActions dbActions)
{
    private readonly ILogger<DbMaintenance> _logger = logger;
    private readonly DbActions _dbActions = dbActions;

    public async Task CleanupLeftoverGuildsAsync(IAsyncEnumerable<DiscordGuild> guilds)
    {
        _logger.DatabaseCleanupStart();

        IEnumerable<ulong> removedGuilds = await _dbActions.DeleteGuildsAsync(guilds);
        await _dbActions.UpdateAzzyBotAsync(lastDatabaseCleanup: true);

        _logger.DatabaseCleanupComplete(removedGuilds.Count());
    }
}
