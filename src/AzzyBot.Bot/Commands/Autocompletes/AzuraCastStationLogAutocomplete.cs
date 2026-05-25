using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Logging;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastStationLogAutocomplete(ILogger<AzuraCastStationLogAutocomplete> logger, IAzuraCastApiService azuraCastApi, ICronJobManager cronJobManager, IDbActions dbActions, IDiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastStationLogAutocomplete> _logger = logger;
    private readonly IAzuraCastApiService _azuraCastApi = azuraCastApi;
    private readonly ICronJobManager _cronJobManager = cronJobManager;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        int stationId = Convert.ToInt32(context.Options.Single(static o => o.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId is 0)
            return [];

        AzuraCastStationEntity? station = await _dbActions.ReadAzuraCastStationAsync(context.Guild.Id, stationId, loadAzuraCast: true, loadAzuraCastPrefs: true);
        if (station is null)
        {
            _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, stationId);
            return [];
        }
        else if (!station.AzuraCast.IsOnline)
        {
            return [];
        }

        Uri baseUrl = new(Crypto.Decrypt(station.AzuraCast.BaseUrl));
        string apiKey = (string.IsNullOrEmpty(station.ApiKey)) ? Crypto.Decrypt(station.AzuraCast.AdminApiKey) : Crypto.Decrypt(station.ApiKey);
        IEnumerable<AzuraSystemLogEntryModel>? stationLogs;
        try
        {
            stationLogs = await _azuraCastApi.GetStationLogsAsync(baseUrl, apiKey, stationId);
            if (stationLogs is null)
            {
                await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the administrative **system logs** endpoint.\n{_azuraCastApi.AzuraCastPermissionsWiki}");
                return [];
            }
        }
        catch (HttpRequestException)
        {
            _cronJobManager.RunAzuraStatusPingJob(station.AzuraCast);
            return [];
        }

        string? search = context.UserInput;
        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraSystemLogEntryModel log in stationLogs)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !log.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new(log.Name, log.Key));
        }

        return results;
    }
}
