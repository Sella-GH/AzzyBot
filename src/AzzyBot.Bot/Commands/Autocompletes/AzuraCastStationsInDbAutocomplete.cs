using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Logging;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastStationsinDbAutocomplete(ILogger<AzuraCastStationsinDbAutocomplete> logger, IAzuraCastApiService azuraCastApi, IAzuraCastPingService azuraCastPing, IDbActions dbActions, IDiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastStationsinDbAutocomplete> _logger = logger;
    private readonly IAzuraCastApiService _azuraCast = azuraCastApi;
    private readonly IAzuraCastPingService _azuraCastPing = azuraCastPing;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        AzuraCastEntity? azuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadStations: true, loadGuild: true);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return [];
        }
        else if (!azuraCast.IsOnline)
        {
            return [];
        }

        IEnumerable<AzuraCastStationEntity> stationsInDb = azuraCast.Stations;
        if (!stationsInDb.Any())
            return [];

        string? search = context.UserInput;
        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));
        string adminApiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraCastStationEntity station in stationsInDb)
        {
            if (results.Count is 25)
                break;

            string stationApiKey = (string.IsNullOrEmpty(station.ApiKey)) ? adminApiKey : Crypto.Decrypt(station.ApiKey);

            AzuraStationRecord? azuraStation;
            try
            {
                azuraStation = await _azuraCast.GetStationAsync(baseUrl, stationApiKey, station.StationId);
                if (azuraStation is null)
                {
                    await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** (ID: {station.StationId}) endpoint.\n{_azuraCast.AzuraCastPermissionsWiki}");
                    return results;
                }
            }
            catch (HttpRequestException)
            {
                await _azuraCastPing.PingInstanceAsync(azuraCast);
                return results;
            }

            if (!string.IsNullOrWhiteSpace(search) && azuraStation.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            AzuraAdminStationConfigRecord? config = await _azuraCast.GetStationAdminConfigAsync(baseUrl, adminApiKey, station.StationId);
            if (config is null)
            {
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint.\n{_azuraCast.AzuraCastPermissionsWiki}");
                return results;
            }

            switch (context.Command.Name)
            {
                case "play-mount":
                    if (config.IsEnabled)
                        results.Add(new(azuraStation.Name, station.StationId));

                    break;

                case "start-station" when config.IsEnabled:
                case "stop-station" when !config.IsEnabled:
                    continue;

                case "start-station" when !config.IsEnabled:
                case "stop-station" when config.IsEnabled:
                    results.Add(new($"{azuraStation.Name} ({Misc.GetReadableBool(config.IsEnabled, ReadableBool.StartedStopped, true)})", station.StationId));
                    break;

                case "toggle-song-requests":
                    results.Add(new($"{azuraStation.Name} ({Misc.GetReadableBool(config.EnableRequests, ReadableBool.EnabledDisabled, true)})", station.StationId));
                    break;

                default:
                    results.Add(new(azuraStation.Name, station.StationId));
                    break;
            }
        }

        return results;
    }
}
