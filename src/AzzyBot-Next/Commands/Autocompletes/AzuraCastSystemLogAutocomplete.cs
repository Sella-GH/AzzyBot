using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AzzyBot.Commands.Autocompletes;

public sealed class AzuraCastSystemLogAutocomplete(AzuraCastApiService azuraCastApi, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly AzuraCastApiService _azuraCastApi = azuraCastApi;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        Dictionary<string, object> results = [];
        AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
        if (azuraCast is null)
            return results;

        string search = context.UserInput;
        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));
        string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        AzuraSystemLogsRecord systemLogs = await _azuraCastApi.GetSystemLogsAsync(baseUrl, apiKey);
        foreach (AzuraSystemLogEntryRecord log in systemLogs.Logs)
        {
            if (results.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !log.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(log.Name, log.Key);
        }

        return results;
    }
}
