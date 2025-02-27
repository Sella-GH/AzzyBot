using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Bot.Services;
using AzzyBot.Bot.Settings;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class GuildsAutocomplete(IOptions<AzzyBotSettings> settings, DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly AzzyBotSettings _settings = settings.Value;
    private readonly DiscordBotService _botService = botService;

    public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
        if (guilds.Count is 0)
            return ValueTask.FromResult(new List<DiscordAutoCompleteChoice>(1).AsEnumerable());

        string? search = context.UserInput;

        List<DiscordAutoCompleteChoice> results = new(25);
        string commandName = context.Command.Name;
        foreach (DiscordGuild guild in guilds.Values)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !guild.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            if (guild.Id == _settings.ServerId && commandName is not "get-joined-server")
                continue;

            results.Add(new(guild.Name, guild.Id.ToString(CultureInfo.InvariantCulture)));
        }

        return ValueTask.FromResult(results.AsEnumerable());
    }
}
