using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzzyHelpAutocomplete(AzzyBotSettingsRecord settings) : IAutoCompleteProvider
{
    private readonly AzzyBotSettingsRecord _settings = settings;

    public ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Client.CurrentApplication.Owners, nameof(context.Client.CurrentApplication.Owners));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));
        ArgumentNullException.ThrowIfNull(context.Member, nameof(context.Member));

        string search = context.UserInput;

        IEnumerable<DiscordUser> botOwners = context.Client.CurrentApplication.Owners;
        ulong guildId = context.Guild.Id;
        DiscordMember member = context.Member;

        bool adminServer = false;
        foreach (DiscordUser _ in botOwners.Where(u => u.Id == context.User.Id && member.Permissions.HasPermission(DiscordPermissions.Administrator) && guildId == _settings.ServerId))
        {
            adminServer = true;
            break;
        }

        bool approvedDebug = guildId == _settings.ServerId;
        Dictionary<string, object> results = new(25);
        foreach (KeyValuePair<string, List<AzzyHelpRecord>> kvp in AzzyHelp.GetAllCommands(context.Extension.Commands, adminServer, approvedDebug, member))
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && kvp.Value.All(r => !r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (AzzyHelpRecord record in kvp.Value.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                if (results.Count is 25)
                    break;

                results.Add(record.Name, record.Name);
            }
        }

        return new ValueTask<IReadOnlyDictionary<string, object>>(results);
    }
}
