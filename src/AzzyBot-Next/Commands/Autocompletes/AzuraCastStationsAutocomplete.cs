﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Enums;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AzzyBot.Commands.Autocompletes;

public sealed class AzuraCastStationsAutocomplete(AzuraCastApiService azuraCastApi, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly AzuraCastApiService _azuraCast = azuraCastApi;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        // TODO Solve this more clean and nicer when it's possible
        Dictionary<string, object> results = [];
        AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
        if (azuraCast is null)
            return results;

        IReadOnlyList<AzuraCastStationEntity> stationsInDb;
        try
        {
            stationsInDb = await _dbActions.GetAzuraCastStationsAsync(context.Guild.Id);
            if (stationsInDb.Count is 0)
                return results;
        }
        catch (InvalidOperationException)
        {
            return results;
        }

        string search = context.UserInput;
        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));
        string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        foreach (AzuraCastStationEntity station in stationsInDb)
        {
            if (results.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !Crypto.Decrypt(station.Name).Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            AzuraAdminStationConfigRecord config = await _azuraCast.GetStationAdminConfigAsync(baseUrl, apiKey, station.StationId);

            switch (context.Command.Name)
            {
                case "start-station" when config.IsEnabled:
                case "stop-station" when !config.IsEnabled:
                    continue;

                case "start-station" when !config.IsEnabled:
                case "stop-station" when config.IsEnabled:
                    results.Add($"{Crypto.Decrypt(station.Name)} ({Misc.ReadableBool(config.IsEnabled, ReadbleBool.StartedStopped, true)})", station.Id);
                    break;

                case "toggle-song-requests":
                    results.Add($"{Crypto.Decrypt(station.Name)} ({Misc.ReadableBool(config.EnableRequests, ReadbleBool.EnabledDisabled, true)})", station.Id);
                    break;

                default:
                    results.Add($"{Crypto.Decrypt(station.Name)}", station.Id);
                    break;
            }
        }

        return results;
    }
}
