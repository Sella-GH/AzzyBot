using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastStationsAutocomplete(ILogger<AzuraCastStationsAutocomplete> logger, AzuraCastApiService azuraCastApi, AzuraCastPingService azuraCastPing, DbActions dbActions, DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastStationsAutocomplete> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCastApi;
    private readonly AzuraCastPingService _azuraCastPing = azuraCastPing;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

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
                    await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** (ID: {station.StationId}) endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
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
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return results;
            }

            switch (context.Command.Name)
            {
                case "play-mount":
                    if (config.IsEnabled)
                        results.Add(new(azuraStation.Name, station));

                    break;

                case "start-station" when config.IsEnabled:
                case "stop-station" when !config.IsEnabled:
                    continue;

                case "start-station" when !config.IsEnabled:
                case "stop-station" when config.IsEnabled:
                    results.Add(new($"{azuraStation.Name} ({Misc.GetReadableBool(config.IsEnabled, ReadableBool.StartedStopped, true)})", station));
                    break;

                case "toggle-song-requests":
                    results.Add(new($"{azuraStation.Name} ({Misc.GetReadableBool(config.EnableRequests, ReadableBool.EnabledDisabled, true)})", station));
                    break;

                default:
                    results.Add(new(azuraStation.Name, station));
                    break;
            }
        }

        return results;
    }
}
