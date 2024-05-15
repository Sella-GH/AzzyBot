using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using DSharpPlus;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzzyBot.Commands.Autocompletes;

internal sealed class GuildsAutocomplete(DiscordShardedClient shardedClient, IDbContextFactory<AzzyDbContext> dbContextFactory) : IAutoCompleteProvider
{
    private readonly DiscordShardedClient _shardedClient = shardedClient;
    private readonly IDbContextFactory<AzzyDbContext> _dbContextFactory = dbContextFactory;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        await using AzzyDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        List<GuildsEntity> guildsInDb = [];
        Dictionary<ulong, string> guilds = [];

        foreach (DiscordInteractionDataOption option in context.Options)
        {
            if (option.Name == "command")
            {
                // 0 = View, 1 = Add, 2 = Remove
                if (option.Value is 0 or 2)
                {
                    guildsInDb = await dbContext.Guilds.Where(g => g.IsDebugAllowed).ToListAsync();
                }
                else
                {
                    guildsInDb = await dbContext.Guilds.Where(g => !g.IsDebugAllowed).ToListAsync();
                }
            }
        }

        // Add all guilds from all shards
        foreach (KeyValuePair<int, DiscordClient> kvp in _shardedClient.ShardClients)
        {
            foreach (KeyValuePair<ulong, DiscordGuild> guild in kvp.Value.Guilds)
            {
                guilds.Add(guild.Key, guild.Value.Name);
            }
        }

        Dictionary<string, object> results = [];
        foreach (GuildsEntity guildDb in guildsInDb)
        {
            if (guilds.TryGetValue(guildDb.UniqueId, out string? value))
                results.Add(value, guildDb.UniqueId);
        }

        return results;
    }
}
