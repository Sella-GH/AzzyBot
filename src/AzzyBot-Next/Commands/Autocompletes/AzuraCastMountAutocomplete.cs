using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AzzyBot.Commands.Autocompletes;

public sealed class AzuraCastMountAutocomplete(DbActions dbActions) : IAutoCompleteProvider
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        Dictionary<string, object> results = [];
        int stationId = Convert.ToInt32(context.Options.Single(o => o.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId == 0)
            return results;

        // TODO Solve this more clean and nicer when it's possible
        IReadOnlyList<AzuraCastStationMountEntity> mountsInDb;
        try
        {
            mountsInDb = await _dbActions.GetAzuraCastStationMountsAsync(context.Guild.Id, stationId);
            if (mountsInDb.Count is 0)
                return results;
        }
        catch (InvalidOperationException)
        {
            return results;
        }

        string search = context.UserInput;
        foreach (AzuraCastStationMountEntity mount in mountsInDb)
        {
            if (results.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !Crypto.Decrypt(mount.Name).Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(Crypto.Decrypt(mount.Name), mount.Id);
        }

        return results;
    }
}
