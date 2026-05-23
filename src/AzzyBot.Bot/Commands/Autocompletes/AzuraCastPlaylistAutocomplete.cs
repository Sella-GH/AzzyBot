using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastPlaylistAutocomplete(ILogger<AzuraCastPlaylistAutocomplete> logger, IAzuraCastApiService azuraCastApi, IAzuraCastPingService azuraCastPing, IDbActions dbActions, IDiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastPlaylistAutocomplete> _logger = logger;
    private readonly IAzuraCastApiService _azuraCast = azuraCastApi;
    private readonly IAzuraCastPingService _azuraCastPing = azuraCastPing;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        int stationId = Convert.ToInt32(context.Options.Single(static o => o.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId is 0)
            return [];

        AzuraCastStationEntity? station;
        try
        {
            station = await _dbActions.ReadAzuraCastStationAsync(context.Guild.Id, stationId, loadAzuraCast: true, loadAzuraCastPrefs: true);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, stationId);
                return [];
            }
        }
        catch (InvalidOperationException)
        {
            return [];
        }

        bool needState = context.Command.Name is "switch-playlist";
        string? search = context.UserInput;
        string apiKey = (!string.IsNullOrEmpty(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(station.AzuraCast.AdminApiKey);
        Uri baseUrl = new(Crypto.Decrypt(station.AzuraCast.BaseUrl));
        IEnumerable<AzuraPlaylistRecord>? playlists;
        try
        {
            playlists = await _azuraCast.GetPlaylistsAsync(baseUrl, apiKey, stationId);
            if (playlists is null)
            {
                await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **playlists** endpoint on station ({stationId}).\n{_azuraCast.AzuraCastPermissionsWiki}");
                return [];
            }
        }
        catch (HttpRequestException)
        {
            await _azuraCastPing.PingInstanceAsync(station.AzuraCast);
            return [];
        }

        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraPlaylistRecord playlist in playlists)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !playlist.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            if (needState)
            {
                results.Add(new($"{playlist.Name} ({Misc.GetReadableBool(playlist.IsEnabled, ReadableBool.EnabledDisabled, true)})", playlist.Id));
            }
            else
            {
                results.Add(new(playlist.Name, playlist.Id));
            }
        }

        return results;
    }
}
