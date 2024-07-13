using System;
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

namespace AzzyBot.Commands.Autocompletes;

public sealed class GuildsAutocomplete(DbActions dbActions, DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        IReadOnlyList<GuildsEntity> guildsInDb = [];
        IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
        switch (context.Command.FullName)
        {
            case "admin debug-servers add-server":
                guildsInDb = await _dbActions.GetGuildsWithDebugAsync(false);
                break;

            case "admin debug-servers remove-server":
                guildsInDb = await _dbActions.GetGuildsWithDebugAsync(true);
                break;
        }

        string search = context.UserInput;
        Dictionary<string, object> results = [];
        if (context.Command.FullName is "admin get-joined-server" or "admin remove-joined-server")
        {
            foreach (KeyValuePair<ulong, DiscordGuild> guild in guilds)
            {
                if (results.Count == 25)
                    break;

                if (!string.IsNullOrWhiteSpace(search) && !guild.Value.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    continue;

                results.Add(guild.Value.Name, guild.Key.ToString(CultureInfo.InvariantCulture));
            }
        }
        else
        {
            foreach (GuildsEntity guildDb in guildsInDb.Where(g => guilds.ContainsKey(g.UniqueId)))
            {
                if (results.Count == 25)
                    break;

                if (!string.IsNullOrWhiteSpace(search) && !guilds[guildDb.UniqueId].Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    continue;

                results.Add(guilds[guildDb.UniqueId].Name, guildDb.UniqueId.ToString(CultureInfo.InvariantCulture));
            }
        }

        return results;
    }
}
