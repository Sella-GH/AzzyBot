using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Services;
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

public sealed class AzuraCastStationsAutocomplete(ILogger<AzuraCastStationsAutocomplete> logger, AzuraCastApiService azuraCastApi, DbActions dbActions, DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastStationsAutocomplete> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCastApi;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadStations: true);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return new Dictionary<string, object>();
        }

        IEnumerable<AzuraCastStationEntity> stationsInDb = azuraCast.Stations;
        if (!stationsInDb.Any())
            return new Dictionary<string, object>();

        string search = context.UserInput;
        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));
        string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        Dictionary<string, object> results = new(25);
        foreach (int station in stationsInDb.Select(s => s.StationId))
        {
            if (results.Count is 25)
                break;

            AzuraStationRecord? azuraStation = await _azuraCast.GetStationAsync(baseUrl, station);
            if (azuraStation is null)
            {
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** ({station}) endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return new Dictionary<string, object>();
            }

            if (!string.IsNullOrWhiteSpace(search) && azuraStation.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            AzuraAdminStationConfigRecord? config = await _azuraCast.GetStationAdminConfigAsync(baseUrl, apiKey, station);
            if (config is null)
            {
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return new Dictionary<string, object>();
            }

            switch (context.Command.Name)
            {
                case "play-mount":
                    if (config.IsEnabled)
                        results.Add(azuraStation.Name, station);

                    break;

                case "start-station" when config.IsEnabled:
                case "stop-station" when !config.IsEnabled:
                    continue;

                case "start-station" when !config.IsEnabled:
                case "stop-station" when config.IsEnabled:
                    results.Add($"{azuraStation.Name} ({Misc.GetReadableBool(config.IsEnabled, ReadableBool.StartedStopped, true)})", station);
                    break;

                case "toggle-song-requests":
                    results.Add($"{azuraStation.Name} ({Misc.GetReadableBool(config.EnableRequests, ReadableBool.EnabledDisabled, true)})", station);
                    break;

                default:
                    results.Add(azuraStation.Name, station);
                    break;
            }
        }

        return results;
    }
}
