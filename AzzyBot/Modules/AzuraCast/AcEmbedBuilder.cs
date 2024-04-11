using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.AzuraCast.Strings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Structs;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.AzuraCast;

internal static class AcEmbedBuilder
{
    private const string AzuraCastLogo = "https://avatars.githubusercontent.com/u/28115974?s=200&v=4";

    internal static DiscordEmbed BuildMusicServerStatsEmbed(string userName, string userAvatarUrl, string ping, string cpuUsageTotal, string[] cpuUsageCores, double[] cpuUsageTimes, double memoryTotal, double memoryUsed, double memoryCached, double memoryUsedTotal, double diskTotal, double diskUsed, string[] networks, double[] networkRXspeed, double[] networkTXspeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(ping, nameof(ping));
        ArgumentException.ThrowIfNullOrWhiteSpace(cpuUsageTotal, nameof(cpuUsageTotal));

        string cpuUsageTotalDesc = string.Empty;

        for (int i = 0; i < cpuUsageCores.Length; i++)
        {
            cpuUsageTotalDesc += AcStringBuilder.GetEmbedMusicStatsCpuUsageCore(i, cpuUsageCores[i]);
        }

        cpuUsageTotalDesc += AcStringBuilder.GetEmbedMusicStatsCpuUsageAll(cpuUsageTotal);

        string title = AcStringBuilder.GetEmbedMusicStatsTitle;
        Dictionary<string, DiscordEmbedStruct> fields = AcStringBuilder.GetEmbedMusicStatsFields(ping, cpuUsageTotalDesc, cpuUsageTimes[0], cpuUsageTimes[1], cpuUsageTimes[2], memoryUsed, memoryCached, memoryUsedTotal, memoryTotal, diskUsed, diskTotal);
        for (int i = 0; i < networks.Length; i++)
        {
            fields.Add(AcStringBuilder.GetEmbedMusicStatsNetworkTitle(networks[i]), new(nameof(AcStringBuilder.GetEmbedMusicStatsNetworkDesc), AcStringBuilder.GetEmbedMusicStatsNetworkDesc(networkRXspeed[i], networkTXspeed[i]), true));
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, DiscordColor.Red, AzuraCastLogo, string.Empty, string.Empty, fields);
    }

    internal static DiscordEmbed BuildServerIsOfflineEmbed(string userName, string userAvatarUrl, bool isOnline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title;
        DiscordColor color;

        if (!isOnline)
        {
            title = AcStringBuilder.GetEmbedServerStatusOffline;
            color = DiscordColor.Red;
        }
        else
        {
            title = AcStringBuilder.GetEmbedServerStatusOnline;
            color = DiscordColor.Green;
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, color);
    }

    internal static DiscordEmbed BuildServerNotAvailableEmbed(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AcStringBuilder.GetEmbedServerNotAvailableTitle;
        string message = AcStringBuilder.GetEmbedServerNotAvailableDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static DiscordEmbed BuildFilesHaveChangedEmbed(string userName, string userAvatarUrl, int addedNumber, int removedNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AcStringBuilder.GetEmbedFilesChangedTitle;
        string addedText = AcStringBuilder.GetEmbedFilesChangedAdded(addedNumber);
        string removedText = AcStringBuilder.GetEmbedFilesChangedRemoved(removedNumber);
        string message;
        DiscordColor color;

        if (addedNumber > 0 && removedNumber == 0)
        {
            message = addedText;
            color = DiscordColor.Green;
        }
        else if (removedNumber > 0 && addedNumber == 0)
        {
            message = removedText;
            color = DiscordColor.Red;
        }
        else
        {
            message = addedText + "\n";
            message += removedText;
            color = DiscordColor.Orange;
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, color);
    }

    internal static DiscordEmbed BuildUpdatesAvailableEmbed(string userName, string userAvatarUrl, AcUpdateModel model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(model, nameof(model));

        string title = AcStringBuilder.GetEmbedAzuraUpdateTitle;
        string description = AcStringBuilder.GetEmbedAzuraUpdateDesc(model.RollingUpdatesList.Count);

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AcStringBuilder.GetEmbedAzuraCurrentRelease(model.CurrentRelease);
        fields.Add(data.Name, data);

        if (model.CurrentRelease != model.LatestRelease && model.NeedsReleaseUpdate)
        {
            data = AcStringBuilder.GetEmbedAzuraLatestRelease(model.LatestRelease);
            fields.Add(data.Name, data);

            data = AcStringBuilder.GetEmbedAzuraMajorRelease;
            fields.Add(data.Name, data);
        }

        if (model.CanSwitchToStable)
        {
            data = AcStringBuilder.GetEmbedAzuraSwitch;
            fields.Add(data.Name, data);
        }

        if (AcSettings.AutomaticChecksUpdatesShowChangelog)
        {
            // Reverse to be historically correct
            model.RollingUpdatesList.Reverse();

            // Split the changelog if it's too big
            const int MaxCharacters = 1024;
            const int MaxParts = 20;
            const int MaxEmbedLength = 6000;
            StringBuilder updateList = new();
            int partNumber = 1;

            // Count the length of every item in the embed
            bool isTooBig = false;
            int embedLength = title.Length + description.Length;
            foreach (KeyValuePair<string, DiscordEmbedStruct> field in fields)
            {
                embedLength += field.Key.Length + field.Value.Description.Length;
            }

            // Create new dictionary because we need the ability to delete it afterwards
            Dictionary<string, string> kvp = [];

            foreach (string update in model.RollingUpdatesList)
            {
                string newLine = $"- {update}\n";

                // Check if adding the new line exceeds the character limit or maximum parts limit
                if (updateList.Length + newLine.Length < MaxCharacters || partNumber < MaxParts)
                {
                    string key = AcStringBuilder.GetEmbedAzuraChangelogPart(partNumber);
                    string value = updateList.ToString();
                    kvp.Add(key, value);
                    updateList.Clear();
                    partNumber++;

                    embedLength += key.Length + value.Length;

                    if (partNumber > MaxParts || embedLength > MaxEmbedLength || value.Length == 0)
                    {
                        isTooBig = true;
                        break;
                    }
                }
                else
                {
                    isTooBig = true;
                    break;
                }

                // Add the new line to the current part
                updateList.Append(newLine);
            }

            // Add the last part if there's any content left and within the max parts limit
            if (updateList.Length > 0 && partNumber <= MaxParts && !isTooBig)
            {
                foreach (KeyValuePair<string, string> field in kvp)
                {
                    fields.Add(field.Key, new(field.Key, field.Value, false));
                }

                data = AcStringBuilder.GetEmbedAzuraChangelog(partNumber, updateList.ToString());
                fields.Add(data.Name, data);
            }

            // Check if length exceeds and display manual changelog message
            if (isTooBig)
            {
                data = AcStringBuilder.GetEmbedAzuraTooBig(model.NeedsReleaseUpdate);
                fields.Add(data.Name, data);
            }
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, description, userName, userAvatarUrl, DiscordColor.IndianRed, AzuraCastLogo, string.Empty, string.Empty, fields);
    }

    internal static DiscordEmbed BuildPlaylistChangesNotAllowedEmbed(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AcStringBuilder.GetEmbedAzuraPlaylistChangesTitle;
        string message = AcStringBuilder.GetEmbedAzuraPlaylistChangesDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static DiscordEmbed BuildSongRequestsNotAllowedEmbed(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AcStringBuilder.EmbedAzuraSongRequestsTitle;
        string message = AcStringBuilder.EmbedAzuraSongRequestsDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static DiscordEmbed BuildCantRequestThisSong(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AcStringBuilder.EmbedAzuraSongInQueueTitle;
        string message = AcStringBuilder.EmbedAzuraSongInQueueDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static async Task<DiscordEmbed> BuildNowPlayingEmbedAsync(string userName, string userAvatarUrl, NowPlayingData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        string title = AcStringBuilder.GetEmbedAzuraNowPlayingTitle;
        string message = string.Empty;
        string thumbnailUrl = (!string.IsNullOrWhiteSpace(data.Live.Art)) ? data.Live.Art : data.Now_Playing.Song.Art;
        string footerText = string.Empty;

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct dataStruct = AcStringBuilder.GetEmbedAzuraNowPlayingSong(data.Now_Playing.Song.Title);
        fields.Add(dataStruct.Name, dataStruct);

        dataStruct = AcStringBuilder.GetEmbedAzuraNowPlayingArtist(data.Now_Playing.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(dataStruct.Name, dataStruct);

        if (!string.IsNullOrWhiteSpace(data.Now_Playing.Song.Album))
        {
            dataStruct = AcStringBuilder.GetEmbedAzuraNowPlayingAlbum(data.Now_Playing.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(dataStruct.Name, dataStruct);
        }

        // If there's a live stream
        if (data.Live.Is_live)
        {
            message = AcStringBuilder.GetEmbedAzuraNowPlayingStreamMessage(data.Live.Streamer_Name);
            dataStruct = AcStringBuilder.GetEmbedAzuraNowPlayingStreamSince(data.Live.Broadcast_Start ?? 0);
            fields.Add(dataStruct.Name, dataStruct);
        }
        // And if not
        else
        {
            TimeSpan timeSpanDuration = TimeSpan.FromSeconds(data.Now_Playing.Duration);
            TimeSpan timeSpanElapsed = TimeSpan.FromSeconds(data.Now_Playing.Elapsed);

            string songDuration = timeSpanDuration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string songElapsed = timeSpanElapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string duration = AcMisc.GetProgressBar(14, data.Now_Playing.Elapsed, data.Now_Playing.Duration);

            dataStruct = AcStringBuilder.GetEmbedAzuraNowPlayingDuration(duration, songElapsed, songDuration);
            fields.Add(dataStruct.Name, dataStruct);

            if (AcSettings.ShowPlaylistsInNowPlaying)
            {
                List<AcPlaylistModel> playlists = await AcServer.GetPlaylistsAsync();
                if (playlists.Count is not 0)
                {
                    foreach (AcPlaylistModel playlist in playlists)
                    {
                        if (playlist.Name == data.Now_Playing.Playlist && !AzuraCastModule.CheckIfDeniedPlaylist(playlist.Id))
                        {
                            dataStruct = AcStringBuilder.GetEmbedAzuraNowPlayingPlaylist(data.Now_Playing.Playlist.Replace($"({AcPlaylistKeywordsEnum.NOREQUESTS})", string.Empty, StringComparison.InvariantCultureIgnoreCase).Trim());
                            fields.Add(dataStruct.Name, dataStruct);

                            break;
                        }
                    }
                }
            }
        }

        if (!AzuraCastModule.CheckIfNowPlayingSloganShouldChange())
        {
            footerText = AcStringBuilder.GetEmbedAzuraNowPlayingFooterClosed;
        }
        else
        {
            PlaylistSloganStruct slogans = await AcServer.GetCurrentSloganAsync(data);

            if (!string.IsNullOrWhiteSpace(slogans.Slogan))
            {
                dataStruct = AcStringBuilder.GetEmbedAzuraNowPlayingSlogan(slogans.Slogan);
                fields.Add(dataStruct.Name, dataStruct);
            }

            if (!string.IsNullOrWhiteSpace(slogans.ListenerSlogan))
            {
                // Check if 0
                // then show only slogan
                // otherwise show listeners and slogan
                footerText = (slogans.Listeners == 0) ? slogans.ListenerSlogan : $"{slogans.Listeners} {slogans.ListenerSlogan}";
            }
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.Aquamarine, thumbnailUrl, footerText, string.Empty, fields);
    }

    internal static DiscordEmbed BuildSearchSongRequestsEmbed(string userName, string userAvatarUrl, List<AcSongRequestsModel> songRequests)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AcStringBuilder.GetEmbedAzuraSearchSongRequestsTitle;
        string message;
        string thumbnailUrl = string.Empty;
        string footerText = string.Empty;
        DiscordColor color;
        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data;

        // if song is found
        if (songRequests.Count == 1)
        {
            SongDetailed song = songRequests[0].Song;

            message = AcStringBuilder.GetEmbedAzuraSearchSongRequestsAvaDesc;
            thumbnailUrl = song.Art;
            color = DiscordColor.SpringGreen;

            data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsAvaSong(song.Title);
            fields.Add(data.Name, data);

            data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsAvaArtist(song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(data.Name, data);

            data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsAvaAlbum(song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(data.Name, data);
        }
        // if explicit song is not found but some others
        else if (songRequests.Count > 1)
        {
            message = AcStringBuilder.GetEmbedAzuraSearchSongRequestsFoundDesc;
            color = DiscordColor.Orange;

            int counter = 0;
            foreach (AcSongRequestsModel songRequest in songRequests)
            {
                counter++;
                SongDetailed song = songRequest.Song;

                string artist = song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase);
                string album = song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase);
                data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsFoundInfo(counter, song.Title, artist, album);
                fields.Add(data.Name, data);
            }

            footerText = AcStringBuilder.GetEmbedAzuraSearchSongRequestsFoundFooter;
        }
        // if nothing is found
        else
        {
            message = AcStringBuilder.GetEmbedAzuraSearchSongRequestsNotDesc;
            color = DiscordColor.IndianRed;
            footerText = AcStringBuilder.GetEmbedAzuraSearchSongRequestsNotFooter;
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, color, thumbnailUrl, footerText, string.Empty, fields);
    }

    internal static DiscordEmbed BuildRequestNotAvailableEmbed(string userName, string userAvatarUrl, string songName, string songArtist = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        string title = AcStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaTitle;
        string message = AcStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaDesc(userName);

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaSong(songName);
        fields.Add(data.Name, data);

        if (!string.IsNullOrWhiteSpace(songArtist))
        {
            data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaArtist(songArtist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(data.Name, data);
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.Orange, string.Empty, string.Empty, string.Empty, fields);
    }

    internal static DiscordEmbed BuildRequestSongEmbed(string userName, string userAvatarUrl, AcSongRequestsModel songRequest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(songRequest, nameof(songRequest));

        string title = AcStringBuilder.GetEmbedAzuraSearchSongRequestsRequestTitle;
        string message = AcStringBuilder.GetEmbedAzuraSearchSongRequestsRequestDesc;
        string thumbnailUrl = songRequest.Song.Art;

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsRequestSong(songRequest.Song.Title);
        fields.Add(data.Name, data);

        data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsRequestArtist(songRequest.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        data = AcStringBuilder.GetEmbedAzuraSearchSongRequestsRequestAlbum(songRequest.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, thumbnailUrl, string.Empty, string.Empty, fields);
    }

    internal static DiscordEmbed BuildFavouriteSongEmbed(string userName, string userAvatarUrl, string songName, string songArtist, string songAlbum, string songArt, bool isUser, string favUser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));
        ArgumentException.ThrowIfNullOrWhiteSpace(songArtist, nameof(songArt));
        ArgumentException.ThrowIfNullOrWhiteSpace(songArtist, nameof(songArtist));
        ArgumentException.ThrowIfNullOrWhiteSpace(favUser, nameof(favUser));

        string title = AcStringBuilder.GetEmbedAzuraFavoriteSongTitle(userName);
        string message = AcStringBuilder.GetEmbedAzuraFavoriteSongDescUser(isUser, favUser);

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AcStringBuilder.GetEmbedAzuraFavoriteSongSong(songName);
        fields.Add(data.Name, data);

        data = AcStringBuilder.GetEmbedAzuraFavoriteSongArtist(songArtist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        data = AcStringBuilder.GetEmbedAzuraFavoriteSongAlbum(songAlbum.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, songArt, string.Empty, string.Empty, fields);
    }
}
