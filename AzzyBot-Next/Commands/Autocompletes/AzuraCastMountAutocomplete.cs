using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Utilities.Encryption;
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

        int stationId = Convert.ToInt32(context.Options.Single(o => o.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId == 0)
            return new Dictionary<string, object>();

        // TODO Solve this more clean and nicer when it's possible
        Dictionary<string, object> results = [];
        List<AzuraCastStationMountEntity> mountsInDb;
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

        foreach (AzuraCastStationMountEntity mount in mountsInDb)
        {
            results.Add(Crypto.Decrypt(mount.Name), mount.Id);
        }

        return results;
    }
}
