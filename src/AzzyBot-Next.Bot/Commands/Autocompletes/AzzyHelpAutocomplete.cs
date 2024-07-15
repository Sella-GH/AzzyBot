using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzzyHelpAutocomplete(ILogger<AzzyHelpAutocomplete> logger, AzzyBotSettingsRecord settings, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly ILogger<AzzyHelpAutocomplete> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Client.CurrentApplication.Owners, nameof(context.Client.CurrentApplication.Owners));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));
        ArgumentNullException.ThrowIfNull(context.Member, nameof(context.Member));

        Dictionary<string, object> results = [];
        string search = context.UserInput;

        IEnumerable<DiscordUser> botOwners = context.Client.CurrentApplication.Owners;
        ulong guildId = context.Guild.Id;
        DiscordMember member = context.Member;
        GuildsEntity? guild = await _dbActions.GetGuildAsync(guildId);
        if (guild is null)
        {
            _logger.DatabaseGuildNotFound(guildId);
            return results;
        }

        bool adminServer = false;
        foreach (DiscordUser _ in botOwners.Where(u => u.Id == context.User.Id && member.Permissions.HasPermission(DiscordPermissions.Administrator) && guildId == _settings.ServerId))
        {
            adminServer = true;
            break;
        }

        bool approvedDebug = guild.IsDebugAllowed || guildId == _settings.ServerId;
        foreach (KeyValuePair<string, List<AzzyHelpRecord>> kvp in AzzyHelp.GetAllCommands(context.Extension.Commands, adminServer, approvedDebug, member))
        {
            if (results.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && kvp.Value.All(r => !r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (AzzyHelpRecord record in kvp.Value.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                if (results.Count == 25)
                    break;

                results.Add(record.Name, record.Name);
            }
        }

        return results;
    }
}
