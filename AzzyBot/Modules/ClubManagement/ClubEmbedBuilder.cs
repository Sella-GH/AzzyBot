using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.ClubManagement.Strings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.ClubManagement;

internal static class ClubEmbedBuilder
{
    internal static DiscordEmbed BuildCloseClubEmbed(string userName, string userAvatarUrl, bool inactivity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = ClubManagementStringBuilder.EmbedCloseClubTitle;
        string message = ClubManagementStringBuilder.EmbedCloseClubDesc(userName);

        if (inactivity)
            message += $"\n{ClubManagementStringBuilder.EmbedCloseClubDescInactivity}";

        string footerText = ClubManagementStringBuilder.EmbedCloseClubFooter;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl, footerText);
    }

    internal static DiscordEmbed BuildOpenClubEmbed(string userName, string userAvatarUrl, string slogan = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl);

        string title = ClubManagementStringBuilder.EmbedOpenClubTitle;
        string message = ClubManagementStringBuilder.EmbedOpenClubDesc(userName);

        // Use user-defined slogan otherwise use default one
        string footerText = (!string.IsNullOrWhiteSpace(slogan)) ? slogan : ClubManagementStringBuilder.EmbedOpenClubFooter;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, CoreSettings.LogoUrl, footerText);
    }

    internal static async Task<DiscordEmbed> BuildClubStatisticsEmbedAsync(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl);

        string title = ClubManagementStringBuilder.EmbedClubStatisticsTitle;
        DateTime opening = ClubManagementModule.ClubOpening;
        DateTime closing = ClubManagementModule.ClubClosing;
        long openingTime = CoreMisc.ConvertToUnixTime(opening);
        long closingTime = CoreMisc.ConvertToUnixTime(closing);
        TimeSpan openingDuration = ClubManagementModule.ClubClosing - ClubManagementModule.ClubOpening;
        List<ListenerModel> listeners = await AzuraCastServer.GetListenersAsync(opening, closing);
        List<SongHistory> songHistory = await AzuraCastServer.GetSongHistoryAsync(opening, closing);
        List<PlaylistModel> playlists = await AzuraCastServer.GetPlaylistsAsync();
        int peakListeners = listeners.Count;
        int visitors = 0;
        int songRequestsNumber = 0;
        List<string> playedPlaylists = [];
        //int outgoingTraffic = 0;

        foreach (ListenerModel listener in listeners)
        {
            // Only count visitors who stayed for at least 15 minutes
            if (listener.TimeConnected >= 300)
                visitors++;
        }

        foreach (SongHistory item in songHistory)
        {
            if (item.IsRequest || string.IsNullOrWhiteSpace(item.Playlist))
                songRequestsNumber++;

            if (!string.IsNullOrWhiteSpace(item.Playlist) && !playedPlaylists.Contains(item.Playlist))
            {
                foreach (PlaylistModel playlist in playlists)
                {
                    if (playlist.Name == item.Playlist && !ClubManagementModule.CheckForDeniedPlaylist(playlist.Id))
                    {
                        playedPlaylists.Add(playlist.Name.Replace($"({AzuraCastPlaylistKeywordsEnum.NOREQUESTS})", string.Empty, StringComparison.InvariantCultureIgnoreCase).Trim());
                        break;
                    }
                }
            }
        }

        string playedLists = string.Empty;
        foreach (string playlist in playedPlaylists)
        {
            playedLists += $"\n- {playlist}";
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, null, string.Empty, string.Empty, string.Empty, ClubManagementStringBuilder.EmbedClubStatisticsFields(openingTime, closingTime, openingDuration, peakListeners, visitors, songHistory.Count, songRequestsNumber, playedLists));
    }
}
