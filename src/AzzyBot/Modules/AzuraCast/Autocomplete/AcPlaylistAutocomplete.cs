using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.AzuraCast.Autocomplete;

/// <summary>
/// Fills up the autocomplete for the Commands responsible for playlists.
/// </summary>
internal sealed class AcPlaylistAutocomplete : IAutocompleteProvider
{
    public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        // Fetch playlists and search term
        List<AcPlaylistModel> playlists = await AcServer.GetPlaylistsAsync();
        string? searchTerm = ctx.OptionValue.ToString();
        bool isAzuraCastCommand = ctx.Interaction.Data.Name == "azuracast";
        bool isStaffCommand = ctx.Interaction.Data.Name == "staff";

        List<DiscordAutoCompleteChoice> choice = [];

        // Loop through all playlists
        foreach (AcPlaylistModel playlist in playlists)
        {
            // Stop the counting if we're at 25
            if (choice.Count == 25)
                break;

            // Check if the playlist is equal to all songs, closed or command is staff AND playlist is already active
            // Skip if yes
            if (AzuraCastModule.CheckIfDeniedPlaylist(playlist.Id) || (isStaffCommand && playlist.Is_enabled))
                continue;

            // Check if the user entered a search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Check if the playlist contains the search term, add then and skip
                if (playlist.Name.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (isStaffCommand || isAzuraCastCommand)
                    {
                        choice.Add(new DiscordAutoCompleteChoice(playlist.Name, playlist.Id.ToString(CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        string playlistName = playlist.Name.Replace($"({AcPlaylistKeywordsEnum.NOREQUESTS})", string.Empty, StringComparison.InvariantCultureIgnoreCase).Trim();
                        choice.Add(new DiscordAutoCompleteChoice(playlistName, playlist.Short_name));
                    }
                }
            }
            else
            {
                // If search term is not there just add it
                if (isStaffCommand || isAzuraCastCommand)
                {
                    choice.Add(new DiscordAutoCompleteChoice(playlist.Name, playlist.Id.ToString(CultureInfo.InvariantCulture)));
                }
                else
                {
                    string playlistName = playlist.Name.Replace($"({AcPlaylistKeywordsEnum.NOREQUESTS})", string.Empty, StringComparison.InvariantCultureIgnoreCase).Trim();
                    choice.Add(new DiscordAutoCompleteChoice(playlistName, playlist.Short_name));
                }
            }
        }

        return choice;
    }
}
