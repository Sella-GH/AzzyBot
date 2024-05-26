using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Utilities.Encryption;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

namespace AzzyBot.Commands.Autocompletes;

public sealed class AzuraCastMountAutocomplete(DbActions dbActions) : IAutoCompleteProvider
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        long stationId = 0;
        foreach (DiscordInteractionDataOption option in context.Options.Where(o => o.Name == "station"))
        {
            if (option.Value is not null)
                stationId = (long)option.Value;
        }

        if (stationId == 0)
            return new Dictionary<string, object>();

        List<AzuraCastMountEntity> mountsInDb = await _dbActions.GetAzuraCastMountsAsync(context.Guild.Id, Convert.ToInt32(stationId));

        Dictionary<string, object> results = [];
        foreach (AzuraCastMountEntity mount in mountsInDb)
        {
            results.Add(Crypto.Decrypt(mount.Name), mount.Id);
        }

        return results;
    }
}
