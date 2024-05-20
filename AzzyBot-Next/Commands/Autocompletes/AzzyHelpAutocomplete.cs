using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Records;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace AzzyBot.Commands.Autocompletes;

public sealed class AzzyHelpAutocomplete(AzzyBotSettingsRecord settings, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        Dictionary<string, object> results = [];

        IEnumerable<DiscordUser> botOwners = context.Client.CurrentApplication.Owners ?? throw new InvalidOperationException("Invalid bot owners");
        ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Invalid guild id");
        DiscordMember member = context.Member ?? throw new InvalidOperationException("Invalid member");
        GuildsEntity guild = await _dbActions.GetGuildEntityAsync(guildId);

        bool adminServer = false;
        foreach (DiscordUser _ in botOwners.Where(u => u.Id == context.User.Id && member.Permissions.HasPermission(DiscordPermissions.Administrator) && guildId == _settings.ServerId))
        {
            adminServer = true;
            break;
        }

        bool approvedDebug = guild.IsDebugAllowed || guildId == _settings.ServerId;

        foreach (KeyValuePair<int, List<AzzyHelpRecord>> kvp in AzzyHelp.GetCommands(adminServer, approvedDebug, member))
        {
            foreach (AzzyHelpRecord command in kvp.Value)
            {
                if (results.Count == 25)
                    return results;

                results.Add(command.Name, command.Name);
            }
        }

        return results;
    }
}
