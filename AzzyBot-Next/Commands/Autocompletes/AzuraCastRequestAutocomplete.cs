using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AzzyBot.Commands.Autocompletes;

public sealed class AzuraCastRequestAutocomplete(AzuraCastApiService azuraCast, DbActions dbActions) : IAutoCompleteProvider
{
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        int stationId = Convert.ToInt32(context.Options.Single(o => o.Name is "station_id" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        if (stationId == 0)
            return new Dictionary<string, object>();

        Dictionary<string, object> results = [];
        string search = context.UserInput;

        AzuraCastStationEntity? station;
        try
        {
            station = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, stationId);
            if (station is null)
                return results;
        }
        catch (InvalidOperationException)
        {
            return results;
        }

        string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(station.AzuraCast.AdminApiKey);
        string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
        StringBuilder songResult = new();

        void AddResultsFromSong<T>(IReadOnlyList<T> songs)
        {
            foreach (T song in songs)
            {
                if (results.Count == 25)
                    break;

                if (song is null)
                    continue;

                bool isRequest = song is AzuraRequestRecord;
                string title = (isRequest) ? (song as AzuraRequestRecord)?.Song.Title ?? string.Empty : (song as AzuraFilesRecord)?.Title ?? string.Empty;
                string artist = (isRequest) ? (song as AzuraRequestRecord)?.Song.Artist ?? string.Empty : (song as AzuraFilesRecord)?.Artist ?? string.Empty;
                string album = (isRequest) ? (song as AzuraRequestRecord)?.Song.Album ?? string.Empty : (song as AzuraFilesRecord)?.Album ?? string.Empty;
                string uniqueId = (isRequest) ? (song as AzuraRequestRecord)?.Song.SongId ?? string.Empty : (song as AzuraFilesRecord)?.UniqueId ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(search) && (!title.Contains(search, StringComparison.OrdinalIgnoreCase) && !artist.Contains(search, StringComparison.OrdinalIgnoreCase) && !album.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    continue;

                songResult.Append(CultureInfo.InvariantCulture, $"{title}");
                if (!string.IsNullOrWhiteSpace(artist))
                    songResult.Append(CultureInfo.InvariantCulture, $" - {artist}");

                results.Add(songResult.ToString(), uniqueId);
                songResult.Clear();
            }
        }

        AzuraAdminStationConfigRecord config = await _azuraCast.GetStationAdminConfigAsync(new(baseUrl), apiKey, stationId);
        if (config.EnableRequests)
        {
            IReadOnlyList<AzuraRequestRecord> requests = await _azuraCast.GetRequestableSongsAsync(new(baseUrl), apiKey, stationId);
            AddResultsFromSong(requests);
        }
        else
        {
            IReadOnlyList<AzuraFilesRecord> files = await _azuraCast.GetFilesLocalAsync(station.Id, station.StationId);
            AddResultsFromSong(files);
        }

        return results;
    }
}
