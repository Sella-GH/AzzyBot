using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Structs;
using AzzyBot.Settings.Core;
using AzzyBot.Strings.AzuraCast;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.AzuraCast;

internal static class AzuraCastEmbedBuilder
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
            cpuUsageTotalDesc += AzuraCastStringBuilder.GetEmbedMusicStatsCpuUsageCore(i, cpuUsageCores[i]);
        }

        cpuUsageTotalDesc += AzuraCastStringBuilder.GetEmbedMusicStatsCpuUsageAll(cpuUsageTotal);

        string title = AzuraCastStringBuilder.GetEmbedMusicStatsTitle;
        Dictionary<string, DiscordEmbedStruct> fields = AzuraCastStringBuilder.GetEmbedMusicStatsFields(ping, cpuUsageTotalDesc, cpuUsageTimes[0], cpuUsageTimes[1], cpuUsageTimes[2], memoryUsed, memoryCached, memoryUsedTotal, memoryTotal, diskUsed, diskTotal);
        for (int i = 0; i < networks.Length; i++)
        {
            fields.Add(AzuraCastStringBuilder.GetEmbedMusicStatsNetworkTitle(networks[i]), new(nameof(AzuraCastStringBuilder.GetEmbedMusicStatsNetworkDesc), AzuraCastStringBuilder.GetEmbedMusicStatsNetworkDesc(networkRXspeed[i], networkTXspeed[i]), true));
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, DiscordColor.Red, AzuraCastLogo, string.Empty, fields);
    }

    internal static DiscordEmbed BuildServerIsOfflineEmbed(string userName, string userAvatarUrl, bool isOnline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title;
        DiscordColor color;

        if (!isOnline)
        {
            title = AzuraCastStringBuilder.GetEmbedServerStatusOffline;
            color = DiscordColor.Red;
        }
        else
        {
            title = AzuraCastStringBuilder.GetEmbedServerStatusOnline;
            color = DiscordColor.Green;
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, color);
    }

    internal static DiscordEmbed BuildServerNotAvailableEmbed(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AzuraCastStringBuilder.GetEmbedServerNotAvailableTitle;
        string message = AzuraCastStringBuilder.GetEmbedServerNotAvailableDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static DiscordEmbed BuildFilesHaveChangedEmbed(string userName, string userAvatarUrl, int addedNumber, int removedNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AzuraCastStringBuilder.GetEmbedFilesChangedTitle;
        string addedText = AzuraCastStringBuilder.GetEmbedFilesChangedAdded(addedNumber);
        string removedText = AzuraCastStringBuilder.GetEmbedFilesChangedRemoved(removedNumber);
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

    internal static DiscordEmbed BuildUpdatesAvailableEmbed(string userName, string userAvatarUrl, AzuraCastUpdateModel model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(model, nameof(model));

        string title = AzuraCastStringBuilder.GetEmbedAzuraUpdateTitle;
        string description = AzuraCastStringBuilder.GetEmbedAzuraUpdateDesc(model.RollingUpdatesList.Count);

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AzuraCastStringBuilder.GetEmbedAzuraCurrentRelease(model.CurrentRelease);
        fields.Add(data.Name, data);

        if (model.CurrentRelease != model.LatestRelease && model.NeedsReleaseUpdate)
        {
            data = AzuraCastStringBuilder.GetEmbedAzuraLatestRelease(model.LatestRelease);
            fields.Add(data.Name, data);

            data = AzuraCastStringBuilder.GetEmbedAzuraMajorRelease;
            fields.Add(data.Name, data);
        }

        if (model.CanSwitchToStable)
        {
            data = AzuraCastStringBuilder.GetEmbedAzuraSwitch;
            fields.Add(data.Name, data);
        }

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
                string key = AzuraCastStringBuilder.GetEmbedAzuraChangelogPart(partNumber);
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

            data = AzuraCastStringBuilder.GetEmbedAzuraChangelog(partNumber, updateList.ToString());
            fields.Add(data.Name, data);
        }

        // Check if length exceeds and display manual changelog message
        if (isTooBig)
        {
            data = AzuraCastStringBuilder.GetEmbedAzuraTooBig(model.NeedsReleaseUpdate);
            fields.Add(data.Name, data);
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, description, userName, userAvatarUrl, DiscordColor.IndianRed, AzuraCastLogo, string.Empty, fields);
    }

    internal static DiscordEmbed BuildPlaylistChangesNotAllowedEmbed(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AzuraCastStringBuilder.GetEmbedAzuraPlaylistChangesTitle;
        string message = AzuraCastStringBuilder.GetEmbedAzuraPlaylistChangesDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static DiscordEmbed BuildSongRequestsNotAllowedEmbed(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AzuraCastStringBuilder.EmbedAzuraSongRequestsTitle;
        string message = AzuraCastStringBuilder.EmbedAzuraSongRequestsDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static DiscordEmbed BuildCantRequestThisSong(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AzuraCastStringBuilder.EmbedAzuraSongInQueueTitle;
        string message = AzuraCastStringBuilder.EmbedAzuraSongInQueueDesc;

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.IndianRed, CoreSettings.LogoUrl);
    }

    internal static async Task<DiscordEmbed> BuildNowPlayingEmbedAsync(string userName, string userAvatarUrl, NowPlayingData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        string title = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingTitle;
        string message = string.Empty;
        string thumbnailUrl = (!string.IsNullOrWhiteSpace(data.Live.Art)) ? data.Live.Art : data.Now_Playing.Song.Art;
        string footerText = string.Empty;

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct dataStruct = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingSong(data.Now_Playing.Song.Title);
        fields.Add(dataStruct.Name, dataStruct);

        dataStruct = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingArtist(data.Now_Playing.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(dataStruct.Name, dataStruct);

        if (!string.IsNullOrWhiteSpace(data.Now_Playing.Song.Album))
        {
            dataStruct = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingAlbum(data.Now_Playing.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(dataStruct.Name, dataStruct);
        }

        // If there's a live stream
        if (data.Live.Is_live)
        {
            message = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingStreamMessage(data.Live.Streamer_Name);
            dataStruct = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingStreamSince(data.Live.Broadcast_Start ?? 0);
            fields.Add(dataStruct.Name, dataStruct);
        }
        // And if not
        else
        {
            TimeSpan timeSpanDuration = TimeSpan.FromSeconds(data.Now_Playing.Duration);
            TimeSpan timeSpanElapsed = TimeSpan.FromSeconds(data.Now_Playing.Elapsed);

            string songDuration = timeSpanDuration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string songElapsed = timeSpanElapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string duration = AzuraCastMisc.GetProgressBar(14, data.Now_Playing.Elapsed, data.Now_Playing.Duration);

            dataStruct = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingDuration(duration, songElapsed, songDuration);
            fields.Add(dataStruct.Name, dataStruct);

            List<PlaylistModel> playlists = await AzuraCastServer.GetPlaylistsAsync();
            foreach (PlaylistModel playlist in playlists)
            {
                if (playlist.Name == data.Now_Playing.Playlist && !AzuraCastModule.CheckIfDeniedPlaylist(playlist.Id))
                {
                    dataStruct = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingPlaylist(data.Now_Playing.Playlist.Replace($"({AzuraCastPlaylistKeywordsEnum.NOREQUESTS})", string.Empty, StringComparison.InvariantCultureIgnoreCase).Trim());
                    fields.Add(dataStruct.Name, dataStruct);

                    break;
                }
            }
        }

        if (!AzuraCastModule.CheckIfNowPlayingSloganShouldChange())
        {
            footerText = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingFooterClosed;
        }
        else
        {
            PlaylistSloganStruct slogans = await AzuraCastServer.GetCurrentSloganAsync(data);

            if (!string.IsNullOrWhiteSpace(slogans.Slogan))
            {
                dataStruct = AzuraCastStringBuilder.GetEmbedAzuraNowPlayingSlogan(slogans.Slogan);
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

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.Aquamarine, thumbnailUrl, footerText, fields);
    }

    internal static DiscordEmbed BuildSearchSongRequestsEmbed(string userName, string userAvatarUrl, List<SongRequestsModel> songRequests)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsTitle;
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

            message = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsAvaDesc;
            thumbnailUrl = song.Art;
            color = DiscordColor.SpringGreen;

            data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsAvaSong(song.Title);
            fields.Add(data.Name, data);

            data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsAvaArtist(song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(data.Name, data);

            data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsAvaAlbum(song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(data.Name, data);
        }
        // if explicit song is not found but some others
        else if (songRequests.Count > 1)
        {
            message = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsFoundDesc;
            color = DiscordColor.Orange;

            int counter = 0;
            foreach (SongRequestsModel songRequest in songRequests)
            {
                counter++;
                SongDetailed song = songRequest.Song;

                string artist = song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase);
                string album = song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase);
                data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsFoundInfo(counter, song.Title, artist, album);
                fields.Add(data.Name, data);
            }

            footerText = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsFoundFooter;
        }
        // if nothing is found
        else
        {
            message = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsNotDesc;
            color = DiscordColor.IndianRed;
            footerText = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsNotFooter;
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, color, thumbnailUrl, footerText, fields);
    }

    internal static DiscordEmbed BuildRequestNotAvailableEmbed(string userName, string userAvatarUrl, string songName, string songArtist = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        string title = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaTitle;
        string message = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaDesc(userName);

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaSong(songName);
        fields.Add(data.Name, data);

        if (!string.IsNullOrWhiteSpace(songArtist))
        {
            data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsNotAvaArtist(songArtist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
            fields.Add(data.Name, data);
        }

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.Orange, string.Empty, string.Empty, fields);
    }

    internal static DiscordEmbed BuildRequestSongEmbed(string userName, string userAvatarUrl, SongRequestsModel songRequest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(songRequest, nameof(songRequest));

        string title = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsRequestTitle;
        string message = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsRequestDesc;
        string thumbnailUrl = songRequest.Song.Art;

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsRequestSong(songRequest.Song.Title);
        fields.Add(data.Name, data);

        data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsRequestArtist(songRequest.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        data = AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsRequestAlbum(songRequest.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, thumbnailUrl, string.Empty, fields);
    }

    internal static DiscordEmbed BuildFavouriteSongEmbed(string userName, string userAvatarUrl, string songName, string songArtist, string songAlbum, string songArt, bool isUser, string favUser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));
        ArgumentException.ThrowIfNullOrWhiteSpace(songArtist, nameof(songArt));
        ArgumentException.ThrowIfNullOrWhiteSpace(songArtist, nameof(songArtist));
        ArgumentException.ThrowIfNullOrWhiteSpace(favUser, nameof(favUser));

        string title = AzuraCastStringBuilder.GetEmbedAzuraFavoriteSongTitle(userName);
        string message = AzuraCastStringBuilder.GetEmbedAzuraFavoriteSongDescUser(isUser, favUser);

        Dictionary<string, DiscordEmbedStruct> fields = [];
        DiscordEmbedStruct data = AzuraCastStringBuilder.GetEmbedAzuraFavoriteSongSong(songName);
        fields.Add(data.Name, data);

        data = AzuraCastStringBuilder.GetEmbedAzuraFavoriteSongArtist(songArtist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        data = AzuraCastStringBuilder.GetEmbedAzuraFavoriteSongAlbum(songAlbum.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase));
        fields.Add(data.Name, data);

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, songArt, string.Empty, fields);
    }
}
