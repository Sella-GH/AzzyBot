﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

public sealed class AzuraCastPlaylistAutocomplete(AzuraCastApiService azuraCast, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        int stationId = Convert.ToInt32(context.Options.Single(o => o.Name is "station_id" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId == 0)
            return new Dictionary<string, object>();

        Dictionary<string, object> results = [];
        string search = context.UserInput;

        AzuraCastStationEntity? station;
        try
        {
            station = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, stationId);
            if (station is null)
                return results;
        }
        catch (InvalidOperationException)
        {
            return results;
        }

        string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(station.AzuraCast.AdminApiKey);
        string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
        IReadOnlyList<AzuraPlaylistRecord> playlists = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, stationId);
        foreach (AzuraPlaylistRecord playlist in playlists)
        {
            if (results.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !playlist.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add($"{playlist.Name} ({Misc.ReadableBool(playlist.IsEnabled, ReadbleBool.EnabledDisabled, true)})", playlist.Id);
        }

        return results;
    }
}
