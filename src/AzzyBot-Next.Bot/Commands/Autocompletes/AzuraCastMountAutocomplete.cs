﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastMountAutocomplete(ILogger<AzuraCastMountAutocomplete> logger, AzuraCastApiService azuraCast, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastMountAutocomplete> _logger = logger;
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

        AzuraCastEntity? azuraCastEntity = await _dbActions.GetAzuraCastAsync(context.Guild.Id, false, false, true);
        if (azuraCastEntity is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return results;
        }

        string search = context.UserInput;
        string name = string.Empty;
        AzuraStationRecord record = await _azuraCast.GetStationAsync(new(Crypto.Decrypt(azuraCastEntity.BaseUrl)), stationId);
        bool hlsAvailable = record.HlsUrl is not null;
        int maxMounts = (hlsAvailable) ? 24 : 25;
        foreach (AzuraStationMountRecord mount in record.Mounts)
        {
            if (results.Count == maxMounts)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !mount.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            name = (!mount.Name.Contains("kbps", StringComparison.OrdinalIgnoreCase))
                ? $"{mount.Name} ({mount.Bitrate} kbps - {mount.Format})"
                : mount.Name;

            results.Add(name, mount.Id);
        }

        if ((string.IsNullOrWhiteSpace(search) || search.Contains("hls", StringComparison.OrdinalIgnoreCase)) && hlsAvailable)
            results.Add("HTTP Live Streaming", 0);

        return results;
    }
}
