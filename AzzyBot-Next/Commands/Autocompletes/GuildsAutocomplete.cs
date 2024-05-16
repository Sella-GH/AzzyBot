using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Services;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzzyBot.Commands.Autocompletes;

internal sealed class GuildsAutocomplete(DiscordBotService botService, IDbContextFactory<AzzyDbContext> dbContextFactory) : IAutoCompleteProvider
{
    private readonly DiscordBotService _botService = botService;
    private readonly IDbContextFactory<AzzyDbContext> _dbContextFactory = dbContextFactory;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        await using AzzyDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        List<GuildsEntity> guildsInDb = [];
        Dictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds();

        switch (context.Command.FullName)
        {
            case "admin remove-debug-guild":
                guildsInDb = await dbContext.Guilds.Where(g => g.IsDebugAllowed).ToListAsync();
                break;

            case "admin set-debug-guild":
                guildsInDb = await dbContext.Guilds.Where(g => !g.IsDebugAllowed).ToListAsync();
                break;
        }

        Dictionary<string, object> results = [];
        foreach (GuildsEntity guildDb in guildsInDb.Where(g => guilds.ContainsKey(g.UniqueId)))
        {
            results.Add(guilds[guildDb.UniqueId].Name, guildDb.UniqueId.ToString(CultureInfo.InvariantCulture));
        }

        return results;
    }
}
