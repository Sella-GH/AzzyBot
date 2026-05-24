using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using AzzyBot.Bot.Models;
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

public sealed class AzuraCastMountAutocomplete(ILogger<AzuraCastMountAutocomplete> logger, IAzuraCastApiService azuraCast, IAzuraCastPingService azuraCastPing, IDbActions dbActions, IDiscordBotService botService, IWebRequestService webRequest) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastMountAutocomplete> _logger = logger;
    private readonly IAzuraCastApiService _azuraCast = azuraCast;
    private readonly IAzuraCastPingService _azuraCastPing = azuraCastPing;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;
    private readonly IWebRequestService _webRequest = webRequest;

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

        AzuraStationModel? stationModel = null;
        try
        {
            stationModel = await _azuraCast.GetStationAsync(baseUrl, apiKey, stationId);
            if (stationModel is null)
            {
                await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** ({stationId}) endpoint.\n{_azuraCast.AzuraCastPermissionsWiki}");
                return [];
            }
        }
        catch (Exception e) when (e is HttpRequestException or InvalidOperationException)
        {
            await _azuraCastPing.PingInstanceAsync(station.AzuraCast);
            return [];
        }

        // Try to detect if the bot is already listening to the station
        IEnumerable<AzuraStationListenerModel>? listeners = await _azuraCast.GetStationListenersAsync(baseUrl, apiKey, stationId);
        AzzyIpAddressModel ipAddresses = await _webRequest.GetIpAddressesAsync();
        string? playingMountPoint = listeners?.FirstOrDefault(l => l.Ip == ipAddresses.Ipv4 || l.Ip == ipAddresses.Ipv6)?.MountName;

        // List all available mounts
        string? search = context.UserInput;
        int maxMounts = (stationModel.HlsEnabled) ? 24 : 25;
        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraStationMountModel mount in stationModel.Mounts)
        {
            if (results.Count == maxMounts)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !mount.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            StringBuilder name = new();
            if (!string.IsNullOrEmpty(playingMountPoint) && playingMountPoint == mount.Name)
                name.Append("(Currently Playing) ");

            if (!mount.Name.Contains("kbps", StringComparison.OrdinalIgnoreCase))
            {
                name.Append(CultureInfo.InvariantCulture, $"{mount.Name} ({mount.Bitrate} kbps - {mount.Format})");
            }
            else
            {
                name.Append(mount.Name);
            }

            name.Append(CultureInfo.InvariantCulture, $" - {mount.Listeners.Total} {((mount.Listeners.Total is not 1) ? "Listeners" : "Listener")}");
            if (mount.IsDefault && !stationModel.HlsIsDefault)
                name.Append(" (Default)");

            results.Add(new(name.ToString(), mount.Id));
        }

        if ((string.IsNullOrWhiteSpace(search) || search.Contains("hls", StringComparison.OrdinalIgnoreCase)) && stationModel.HlsEnabled)
        {
            IEnumerable<AzuraHlsMountModel>? hlsMounts = await _azuraCast.GetStationHlsMountPointsAsync(baseUrl, apiKey, stationId);
            if (hlsMounts is null)
                return results;

            int hlsMaxBitrate = hlsMounts.Max(static m => m.Bitrate);
            int hlsMinBitrate = hlsMounts.Min(static m => m.Bitrate);
            int hlsListeners = hlsMounts.Sum(static m => m.Listeners);

            StringBuilder name = new();
            if (!string.IsNullOrEmpty(playingMountPoint) && hlsMounts.Any(m => $"HLS: {m.Name}" == playingMountPoint))
                name.Append("(Currently Playing) ");

            name.Append("HTTP Live Streaming ");
            name.Append(CultureInfo.InvariantCulture, $"({hlsMinBitrate} - {hlsMaxBitrate} kbps - AAC) ");
            name.Append(CultureInfo.InvariantCulture, $"- {hlsListeners} {((hlsListeners is not 1) ? "Listeners" : "Listener")}");
            if (stationModel.HlsIsDefault)
                name.Append(" (Default)");

            results.Add(new(name.ToString(), 0));
        }

        return results;
    }
}
