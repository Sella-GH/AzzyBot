using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Core.Enums;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Logging;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastStationsOnlineAutocomplete(ILogger<AzuraCastStationsOnlineAutocomplete> logger, IAzuraCastApiService azuraCastApi, IDbActions dbActions) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastStationsOnlineAutocomplete> _logger = logger;
    private readonly IAzuraCastApiService _azuraCast = azuraCastApi;
    private readonly IDbActions _dbActions = dbActions;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        AzuraCastEntity? azuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return [];
        }
        else if (!azuraCast.IsOnline)
        {
            return [];
        }

        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));
        string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        IEnumerable<AzuraCastStationEntity> stationsInDb = azuraCast.Stations;
        IEnumerable<AzuraAdminStationConfigModel>? stationsOnline = await _azuraCast.GetStationsAdminConfigAsync(baseUrl, apiKey);
        if (stationsOnline is null)
            return [];

        IEnumerable<AzuraAdminStationConfigModel> stations = stationsOnline.Where(o => !stationsInDb.Any(s => s.StationId == o.Id));
        string? search = context.UserInput;
        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraAdminStationConfigModel station in stations)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !station.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new DiscordAutoCompleteChoice($"{station.Name} ({Misc.GetReadableBool(station.IsEnabled, ReadableBool.EnabledDisabled)})", station.Id));
        }

        return results;
    }
}
