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
        IReadOnlyList<AzuraRequestRecord> requests = await _azuraCast.GetRequestableSongsAsync(new(baseUrl), apiKey, stationId);
        StringBuilder song = new();
        foreach (AzuraRequestRecord request in requests)
        {
            if (results.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && (!request.Song.Title.Contains(search, StringComparison.OrdinalIgnoreCase) && !request.Song.Artist.Contains(search, StringComparison.OrdinalIgnoreCase) && !request.Song.Album.Contains(search, StringComparison.OrdinalIgnoreCase)))
                continue;

            song.Append(CultureInfo.InvariantCulture, $"{request.Song.Title}");
            if (!string.IsNullOrWhiteSpace(request.Song.Artist))
                song.Append(CultureInfo.InvariantCulture, $" - {request.Song.Artist}");

            results.Add(song.ToString(), request.Song.Id);
            song.Clear();
        }

        return results;
    }
}
