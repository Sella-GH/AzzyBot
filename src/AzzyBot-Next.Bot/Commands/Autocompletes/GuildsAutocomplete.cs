using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Bot.Services;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class GuildsAutocomplete(DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly DiscordBotService _botService = botService;

    public ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
        string search = context.UserInput;

        Dictionary<string, object> results = [];
        foreach (KeyValuePair<ulong, DiscordGuild> guild in guilds)
        {
            if (results.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !guild.Value.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(guild.Value.Name, guild.Key.ToString(CultureInfo.InvariantCulture));
        }

        return new ValueTask<IReadOnlyDictionary<string, object>>(results);
    }
}
