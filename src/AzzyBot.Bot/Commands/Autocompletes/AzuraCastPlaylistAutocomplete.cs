﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastPlaylistAutocomplete(ILogger<AzuraCastPlaylistAutocomplete> logger, AzuraCastApiService azuraCast, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastPlaylistAutocomplete> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        Dictionary<string, object> results = [];
        int stationId = Convert.ToInt32(context.Options.Single(o => o.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId is 0)
            return results;

        AzuraCastStationEntity? station;
        try
        {
            station = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, stationId, loadAzuraCast: true);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, stationId);
                return results;
            }
        }
        catch (InvalidOperationException)
        {
            return results;
        }

        bool needState = context.Command.Name is "switch-playlist";
        string search = context.UserInput;
        string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(station.AzuraCast.AdminApiKey);
        string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
        IEnumerable<AzuraPlaylistRecord> playlists = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, stationId);
        foreach (AzuraPlaylistRecord playlist in playlists)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !playlist.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            if (needState)
            {
                results.Add($"{playlist.Name} ({Misc.ReadableBool(playlist.IsEnabled, ReadbleBool.EnabledDisabled, true)})", playlist.Id);
            }
            else
            {
                results.Add(playlist.Name, playlist.Id);
            }
        }

        return results;
    }
}