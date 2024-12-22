using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.Modules;
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

public sealed class AzuraCastRequestAutocomplete(ILogger<AzuraCastRequestAutocomplete> logger, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastRequestAutocomplete> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

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
            station = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, stationId, loadAzuraCast: true, loadAzuraCastPrefs: true);
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

        string? search = context.UserInput;
        string apiKey = (string.IsNullOrEmpty(station.ApiKey)) ? Crypto.Decrypt(station.AzuraCast.AdminApiKey) : Crypto.Decrypt(station.ApiKey);
        string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
        StringBuilder songResult = new();
        List<DiscordAutoCompleteChoice> results = new(25);
        void AddResultsFromSong<T>(IEnumerable<T> songs)
        {
            foreach (T song in songs)
            {
                if (results.Count is 25)
                    break;

                if (song is null)
                    continue;

                string title = string.Empty;
                string artist = string.Empty;
                string uniqueId = string.Empty;
                int requestId = 0;
                switch (song)
                {
                    case AzuraRequestRecord request:
                        title = request.Song.Title ?? string.Empty;
                        artist = request.Song.Artist ?? string.Empty;
                        uniqueId = request.Song.SongId ?? string.Empty;
                        break;

                    case AzuraFilesRecord file:
                        title = file.Title ?? string.Empty;
                        artist = file.Artist ?? string.Empty;
                        uniqueId = file.SongId ?? string.Empty;
                        break;

                    case AzuraRequestQueueItemRecord requestQueueItem:
                        title = requestQueueItem.Track.Title ?? string.Empty;
                        artist = requestQueueItem.Track.Artist ?? string.Empty;
                        requestId = requestQueueItem.Id;
                        break;

                    default:
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(search) && (!title.Contains(search, StringComparison.OrdinalIgnoreCase) && !artist.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    continue;

                songResult.Append(title);
                if (!string.IsNullOrEmpty(artist))
                    songResult.Append(CultureInfo.InvariantCulture, $" - {artist}");

                results.Add(new(songResult.ToString(), (string.IsNullOrEmpty(uniqueId)) ? requestId.ToString(CultureInfo.InvariantCulture) : uniqueId));
                songResult.Clear();
            }
        }

        if (station.AzuraCast.IsOnline && context.Command.Name is "delete-song-request")
        {
            IEnumerable<AzuraRequestQueueItemRecord>? requests = await _azuraCast.GetStationRequestItemsAsync(new(baseUrl), apiKey, stationId, history: false);
            if (requests is null)
            {
                await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **requests** endpoint on station ({stationId}).\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return results;
            }

            AddResultsFromSong(requests);

            return results;
        }

        if (station.AzuraCast.IsOnline)
        {
            AzuraAdminStationConfigRecord? config = await _azuraCast.GetStationAdminConfigAsync(new(baseUrl), apiKey, stationId);
            if (config is null)
            {
                await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return results;
            }

            if (config.EnableRequests)
            {
                IEnumerable<AzuraRequestRecord>? requests = await _azuraCast.GetRequestableSongsAsync(new(baseUrl), apiKey, stationId);
                if (requests is null)
                {
                    await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **requests** endpoint on station ({stationId}).\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                    return results;
                }

                AddResultsFromSong(requests);
            }
            else
            {
                IEnumerable<AzuraFilesDetailedRecord>? filesOnline = await _azuraCast.GetFilesOnlineAsync<AzuraFilesDetailedRecord>(new(baseUrl), apiKey, stationId);
                if (filesOnline is null)
                {
                    await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **files** endpoint on station ({stationId}).\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                    return results;
                }

                IEnumerable<AzuraPlaylistRecord>? playlists = await _azuraCast.GetPlaylistsWithRequestsAsync(new(baseUrl), apiKey, stationId);
                if (playlists is null)
                {
                    await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **playlists** endpoint on station ({stationId}).\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                    return results;
                }

                // Get all files that are in the playlists that have requests enabled
                IEnumerable<AzuraFilesRecord> fileRequests = filesOnline.Where(f => f.Playlists.Any(p => playlists.Any(pl => pl.Id == p.Id)));
                AddResultsFromSong(fileRequests);
            }

            return results;
        }

        IEnumerable<AzuraFilesRecord> filesLocal = await _azuraCast.GetFilesLocalAsync(station.AzuraCast.GuildId, station.AzuraCast.Id, station.Id, station.StationId);
        AddResultsFromSong(filesLocal);

        return results;
    }
}
