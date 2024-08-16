using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Settings;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class GuildsAutocomplete(AzzyBotSettingsRecord settings, DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DiscordBotService _botService = botService;

    public ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
        if (guilds.Count is 0)
            return new ValueTask<IReadOnlyDictionary<string, object>>();

        string search = context.UserInput;

        Dictionary<string, object> results = new(25);
        string commandName = context.Command.Name;
        foreach (DiscordGuild guild in guilds.Values)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !guild.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            if (guild.Id == _settings.ServerId && commandName is not "get-joined-server")
                continue;

            results.Add(guild.Name, guild.Id.ToString(CultureInfo.InvariantCulture));
        }

        return new ValueTask<IReadOnlyDictionary<string, object>>(results);
    }
}
