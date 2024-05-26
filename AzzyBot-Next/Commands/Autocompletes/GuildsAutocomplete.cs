﻿using System;
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

        List<GuildsEntity> guildsInDb = [];
        Dictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds();

        switch (context.Command.FullName)
        {
            case "admin remove-debug-guild":
                guildsInDb = await _dbActions.GetGuildsWithDebugAsync(true);
                break;

            case "admin set-debug-guild":
                guildsInDb = await _dbActions.GetGuildsWithDebugAsync(false);
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
