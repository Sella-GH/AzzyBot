using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Utilities.Encryption;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AzzyBot.Commands.Autocompletes;

public sealed class AzuraCastStationsAutocomplete(DbActions dbActions) : IAutoCompleteProvider
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        // TODO Solve this more clean and nicer when it's possible
        Dictionary<string, object> results = [];
        List<AzuraCastStationEntity> stationsInDb;
        try
        {
            stationsInDb = await _dbActions.GetAzuraCastStationsAsync(context.Guild.Id);
        }
        catch (InvalidOperationException)
        {
            return results;
        }

        foreach (AzuraCastStationEntity station in stationsInDb)
        {
            results.Add(Crypto.Decrypt(station.Name), station.Id);
        }

        return results;
    }
}
