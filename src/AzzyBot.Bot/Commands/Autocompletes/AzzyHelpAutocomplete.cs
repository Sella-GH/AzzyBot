using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Bot.Models;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzzyHelpAutocomplete(IOptions<AzzyBotSettings> settings) : IAutoCompleteProvider
{
    private readonly AzzyBotSettings _settings = settings.Value;

    public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Client.CurrentApplication.Owners);
        ArgumentNullException.ThrowIfNull(context.Guild);
        ArgumentNullException.ThrowIfNull(context.Member);

        IEnumerable<DiscordUser> botOwners = context.Client.CurrentApplication.Owners;
        ulong guildId = context.Guild.Id;
        DiscordMember member = context.Member;

        bool adminServer = botOwners.Any(u => u.Id == context.User.Id && member.Permissions.HasPermission(DiscordPermission.Administrator) && guildId == _settings.ServerId);
        bool approvedDebug = guildId == _settings.ServerId;
        string? search = context.UserInput;
        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (List<AzzyHelpModel> kvp in AzzyHelp.GetAllCommands(context.Extension.Commands, adminServer, approvedDebug, member).Select(k => k.Value))
        {
            if (results.Count is 25)
                break;

            foreach (string model in kvp.Where(r => string.IsNullOrWhiteSpace(search) || r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).Select(r => r.Name))
            {
                if (results.Count is 25)
                    break;

                results.Add(new(model, model));
            }
        }

        return ValueTask.FromResult(results.AsEnumerable());
    }
}
