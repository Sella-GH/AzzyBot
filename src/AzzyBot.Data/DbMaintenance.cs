using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Data;

public sealed class DbMaintenance(ILogger<DbMaintenance> logger, DbActions dbActions)
{
    private readonly ILogger<DbMaintenance> _logger = logger;
    private readonly DbActions _dbActions = dbActions;

    public async Task CleanupLeftoverGuildsAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds)
    {
        _logger.DatabaseCleanupStart();

        IEnumerable<ulong> removedGuilds = await _dbActions.DeleteGuildsAsync(guilds);

        _logger.DatabaseCleanupComplete(removedGuilds.Count());
    }
}
