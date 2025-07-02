using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastMountAutocomplete(ILogger<AzuraCastMountAutocomplete> logger, AzuraCastApiService azuraCast, AzuraCastPingService azuraCastPing, DbActions dbActions, DiscordBotService botService, WebRequestService webRequest) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastMountAutocomplete> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly AzuraCastPingService _azuraCastPing = azuraCastPing;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;
    private readonly WebRequestService _webRequest = webRequest;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        int stationId = Convert.ToInt32(context.Options.Single(static o => o.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId is 0)
            return [];

        AzuraCastStationEntity? stationEntity = await _dbActions.ReadAzuraCastStationAsync(context.Guild.Id, stationId, loadAzuraCast: true, loadAzuraCastPrefs: true);
        if (stationEntity is null)
        {
            _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, stationId);
            return [];
        }
        else if (!stationEntity.AzuraCast.IsOnline)
        {
            return [];
        }

        Uri baseUrl = new(Crypto.Decrypt(stationEntity.AzuraCast.BaseUrl));
        AzuraStationRecord? record = null;
        try
        {
            record = await _azuraCast.GetStationAsync(baseUrl, stationId);
            if (record is null)
            {
                await _botService.SendMessageAsync(stationEntity.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** ({stationId}) endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return [];
            }
        }
        catch (Exception e) when (e is HttpRequestException or InvalidOperationException)
        {
            await _azuraCastPing.PingInstanceAsync(stationEntity.AzuraCast);
            return [];
        }

        // Try to detect if the bot is already listening to the station
        string apiKey = (string.IsNullOrEmpty(stationEntity.ApiKey)) ? Crypto.Decrypt(stationEntity.AzuraCast.AdminApiKey) : Crypto.Decrypt(stationEntity.ApiKey);
        IEnumerable<AzuraStationListenerRecord>? listeners = await _azuraCast.GetStationListenersAsync(baseUrl, apiKey, stationId);
        AzzyIpAddressRecord ipAddresses = await _webRequest.GetIpAddressesAsync();
        string? playingMountPoint = listeners?.FirstOrDefault(l => l.Ip == ipAddresses.Ipv4 || l.Ip == ipAddresses.Ipv6)?.MountName;

        // List all available mounts
        string? search = context.UserInput;
        int maxMounts = (record.HlsEnabled) ? 24 : 25;
        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraStationMountRecord mount in record.Mounts)
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
            if (mount.IsDefault && !record.HlsIsDefault)
                name.Append(" (Default)");

            results.Add(new(name.ToString(), mount.Id));
        }

        if ((string.IsNullOrWhiteSpace(search) || search.Contains("hls", StringComparison.OrdinalIgnoreCase)) && record.HlsEnabled)
        {
            IEnumerable<AzuraHlsMountRecord>? hlsMounts = await _azuraCast.GetStationHlsMountPointsAsync(baseUrl, apiKey, stationId);
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
            if (record.HlsIsDefault)
                name.Append(" (Default)");

            results.Add(new(name.ToString(), 0));
        }

        return results;
    }
}
