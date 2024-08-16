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

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        IAsyncEnumerable<DiscordGuild> guilds = _botService.GetDiscordGuildsAsync;
        string search = context.UserInput;

        Dictionary<string, object> results = new(25);
        await foreach (DiscordGuild guild in guilds)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !guild.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            if (guild.Id == _settings.ServerId)
                continue;

            results.Add(guild.Name, guild.Id.ToString(CultureInfo.InvariantCulture));
        }

        return results;
    }
}
