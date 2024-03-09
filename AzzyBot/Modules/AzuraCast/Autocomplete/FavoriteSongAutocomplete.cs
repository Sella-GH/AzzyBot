using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.Core;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Autocomplete;

/// <summary>
/// Fills up the autocomplete for the FavoriteSong command.
/// </summary>
internal sealed class FavoriteSongAutocomplete : IAutocompleteProvider
{
    public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        if (AzuraCastModule.FavoriteSongsLock is null)
            throw new InvalidOperationException($"{nameof(AzuraCastModule.FavoriteSongsLock)} is null");

        string json = await AzuraCastModule.FavoriteSongsLock.GetFileContentAsync();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("json is empty");

        string? searchTerm = ctx.OptionValue.ToString();
        FavoriteSongModel? userIds = JsonConvert.DeserializeObject<FavoriteSongModel>(json);

        if (userIds is null)
            throw new InvalidOperationException($"{nameof(userIds)} is null");

        List<DiscordAutoCompleteChoice> choice = [];

        for (int i = 0; i < userIds.UserSongList.Count; i++)
        {
            // Stop the counting if we're at 25
            if (choice.Count == 25)
                break;

            if (string.IsNullOrWhiteSpace(userIds.UserSongList[i].SongId) || userIds.UserSongList[i].UserId == 0)
                continue;

            DiscordMember member = await CoreDiscordCommands.GetMemberAsync(userIds.UserSongList[i].UserId, ctx.Guild);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                if (member.DisplayName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) || member.Nickname.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) || member.Username.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                {
                    choice.Add(new DiscordAutoCompleteChoice(CoreDiscordCommands.GetBestUsername(member.Username, member.Nickname), member.Id.ToString(CultureInfo.InvariantCulture)));
                }
            }
            else
            {
                choice.Add(new DiscordAutoCompleteChoice(CoreDiscordCommands.GetBestUsername(member.Username, member.Nickname), member.Id.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return choice;
    }
}
