using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

public sealed class AzuraCastRequestAutocomplete(ILogger<AzuraCastRequestAutocomplete> logger, AzuraCastApiService azuraCast, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastRequestAutocomplete> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        int stationId = Convert.ToInt32(context.Options.Single(o => o.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId is 0)
            return new Dictionary<string, object>();

        AzuraCastStationEntity? station;
        try
        {
            station = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, stationId, loadAzuraCast: true);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, stationId);
                return new Dictionary<string, object>();
            }
        }
        catch (InvalidOperationException)
        {
            return new Dictionary<string, object>();
        }

        string search = context.UserInput;
        string apiKey = (string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.AzuraCast.AdminApiKey) : Crypto.Decrypt(station.ApiKey);
        string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
        StringBuilder songResult = new();
        Dictionary<string, object> results = new(25);
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
                if (song is AzuraRequestRecord request)
                {
                    title = request.Song.Title ?? string.Empty;
                    artist = request.Song.Artist ?? string.Empty;
                    uniqueId = request.Song.SongId ?? string.Empty;
                }
                else if (song is AzuraFilesRecord file)
                {
                    title = file.Title ?? string.Empty;
                    artist = file.Artist ?? string.Empty;
                    uniqueId = file.UniqueId ?? string.Empty;
                }
                else if (song is AzuraRequestQueueItemRecord requestQueueItem)
                {
                    title = requestQueueItem.Track.Title ?? string.Empty;
                    artist = requestQueueItem.Track.Artist ?? string.Empty;
                    requestId = requestQueueItem.Id;
                }

                if (!string.IsNullOrWhiteSpace(search) && (!title.Contains(search, StringComparison.OrdinalIgnoreCase) && !artist.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    continue;

                songResult.Append(CultureInfo.InvariantCulture, $"{title}");
                if (!string.IsNullOrWhiteSpace(artist))
                    songResult.Append(CultureInfo.InvariantCulture, $" - {artist}");

                results.Add(songResult.ToString(), (string.IsNullOrWhiteSpace(uniqueId)) ? requestId : uniqueId);
                songResult.Clear();
            }
        }

        if (context.Command.Name is "delete-song-request")
        {
            IEnumerable<AzuraRequestQueueItemRecord> requests = await _azuraCast.GetStationRequestItemsAsync(new(baseUrl), apiKey, stationId, false);
            AddResultsFromSong(requests);

            return results;
        }

        if (station.AzuraCast.IsOnline)
        {
            AzuraAdminStationConfigRecord config = await _azuraCast.GetStationAdminConfigAsync(new(baseUrl), apiKey, stationId);
            if (config.EnableRequests)
            {
                IEnumerable<AzuraRequestRecord> requests = await _azuraCast.GetRequestableSongsAsync(new(baseUrl), apiKey, stationId);
                AddResultsFromSong(requests);
            }
            else
            {
                IEnumerable<AzuraFilesDetailedRecord> filesOnline = await _azuraCast.GetFilesOnlineAsync<AzuraFilesDetailedRecord>(new(baseUrl), apiKey, stationId);
                IEnumerable<AzuraPlaylistRecord> playlists = await _azuraCast.GetPlaylistsWithRequestsAsync(new(baseUrl), apiKey, stationId);
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
