using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Structs;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.AzuraCast;

internal static class AcServer
{
    internal static readonly Dictionary<string, string> Headers = new()
    {
        ["accept"] = "application/json",
        ["X-API-Key"] = AcSettings.AzuraApiKey
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    internal static async Task<NowPlayingData> GetNowPlayingAsync()
    {
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.nowplaying, AcSettings.AzuraStationKey);
        string body = await CoreWebRequests.GetWebAsync(url, null, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        NowPlayingData data = JsonSerializer.Deserialize<NowPlayingData>(body) ?? throw new InvalidOperationException($"{nameof(data)} is null");

        return data;
    }

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    internal static async Task<AcStationModel> GetStationDataAsync()
    {
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey);
        string body = await CoreWebRequests.GetWebAsync(url, null, AcSettings.Ipv6Available);
        if (string.IsNullOrEmpty(body))
            throw new InvalidOperationException("body is empty");

        AcStationModel station = JsonSerializer.Deserialize<AcStationModel>(body) ?? throw new InvalidOperationException($"{nameof(station)} is null");

        return station;
    }

    internal static async Task<PlaylistSloganStruct> GetCurrentSloganAsync(NowPlayingData nowPlaying)
    {
        ArgumentNullException.ThrowIfNull(nowPlaying, nameof(nowPlaying));

        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.AzuraCast)];
        string configBody = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.PlaylistSlogansJSON), directories);
        if (string.IsNullOrWhiteSpace(configBody))
            throw new InvalidOperationException("configBody is empty");

        AcConfigSlogansModel playlistSlogan = JsonSerializer.Deserialize<AcConfigSlogansModel>(configBody) ?? throw new InvalidOperationException($"{nameof(playlistSlogan)} & {nameof(nowPlaying)} are null");

        bool isLive = nowPlaying.Live.Is_live;
        string playlist = nowPlaying.Now_Playing.Playlist;
        int listeners = nowPlaying.Listeners.Current;

        // Every basic assumption is we deal with a live stream
        SloganContent sloganContent = playlistSlogan.Slogans.LiveStreamListener;
        string slogan = playlistSlogan.Slogans.LiveStream;

        // Assume we are dealing with a song request
        if (!isLive && string.IsNullOrWhiteSpace(playlist))
        {
            sloganContent = playlistSlogan.Slogans.SongRequestListener;
            slogan = playlistSlogan.Slogans.SongRequests;
        }

        // Default playlist
        if (!string.IsNullOrWhiteSpace(playlist))
        {
            // Not a song request, assume we have no user defined
            slogan = playlistSlogan.Slogans.DefaultSlogan;
            sloganContent = playlistSlogan.Slogans.DefaultSloganListener;

            // Loop the playlistSlogan to get the correct one
            // Check if the keyword is included in the playlist
            for (int i = 0; i < playlistSlogan.Slogans.UserDefined.Count; i++)
            {
                if (playlist.Contains(playlistSlogan.Slogans.UserDefined[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    // We do have a user defined so use that instead
                    slogan = playlistSlogan.Slogans.UserDefined[i].Slogan;
                    sloganContent = playlistSlogan.Slogans.UserDefined[i];
                    break;
                }
            }
        }

        // Return correct slogan based on listener count
        return listeners switch
        {
            0 => new(slogan, sloganContent.None, 0),
            1 => new(slogan, sloganContent.OnePerson, 1),
            2 => new(slogan, sloganContent.TwoPersons, 2),
            _ => new(slogan, sloganContent.Multiple, listeners)
        };
    }

    internal static async Task<bool> CheckIfSongRequestsAreAllowedAsync()
    {
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.admin, AcApiEnum.station, AcSettings.AzuraStationKey);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        AcStationConfigModel config = JsonSerializer.Deserialize<AcStationConfigModel>(body) ?? throw new InvalidOperationException($"{nameof(config)} is null");

        if (!config.Enable_requests)
            return false;

        NowPlayingData nowPlaying = await GetNowPlayingAsync();
        string playlist = nowPlaying.Now_Playing.Playlist;

        return !playlist.Contains(nameof(AcPlaylistKeywordsEnum.NOREQUESTS), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> CheckIfSongIsQueuedAsync(string id, string songName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        // Get the song request queue first
        List<AcSongRequestsQueueModel> requestQueue = await GetSongRequestsQueuesAsync();

        // Then get the regular queue after
        List<AcQueueItemModel> queue = await GetQueueAsync();

        // Lastly get the song request history
        List<AcSongRequestsQueueModel> history = await GetSongRequestsHistoryAsync(songName);

        // First check the regular queue if the song is there
        foreach (AcQueueItemModel item in queue)
        {
            if (item.Song.Id == id)
                return true;
        }

        // Then check the song request history if the song was requested in the last 10 minutes
        // The 10 minute stuff comes from AzuraCast, can't change it
        foreach (AcSongRequestsQueueModel historyItem in history)
        {
            if (historyItem.Track.Song_Id == id)
            {
                long diff = CoreMisc.ConvertToUnixTime(DateTime.Now) - historyItem.Timestamp;

                if (diff <= 600)
                    return true;
            }
        }

        // Lastly check the song request queue if the song is there
        foreach (AcSongRequestsQueueModel requestItem in requestQueue)
        {
            if (requestItem.Track.Song_Id == id)
                return true;
        }

        return false;
    }

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    private static async Task<List<AcSongRequestsQueueModel>> GetSongRequestsQueuesAsync()
    {
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.reports, AcApiEnum.requests);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcSongRequestsQueueModel> requestQueue = JsonSerializer.Deserialize<List<AcSongRequestsQueueModel>>(body) ?? throw new InvalidOperationException($"{nameof(requestQueue)} is null");

        return requestQueue;
    }

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    private static async Task<List<AcQueueItemModel>> GetQueueAsync()
    {
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.queue);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcQueueItemModel> queue = JsonSerializer.Deserialize<List<AcQueueItemModel>>(body) ?? throw new InvalidOperationException($"{nameof(queue)} is null");

        return queue;
    }

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    private static async Task<List<AcSongRequestsQueueModel>> GetSongRequestsHistoryAsync(string songName = "")
    {
        string url = $"{string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.reports, AcApiEnum.requests)}?{AcApiEnum.type}={AcApiEnum.history}&{AcApiEnum.searchPhrase}={songName.Replace(" ", "+", StringComparison.InvariantCultureIgnoreCase)}";
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcSongRequestsQueueModel> history = JsonSerializer.Deserialize<List<AcSongRequestsQueueModel>>(body) ?? throw new InvalidOperationException($"{nameof(history)} is null");

        return history;
    }

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    internal static async Task<List<SongHistory>> GetSongHistoryAsync(DateTime startTime, DateTime endTime)
    {
        string url = $"{string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.history)}?{AcApiEnum.start}={startTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}&{AcApiEnum.end}={endTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}".Replace("_", "T", StringComparison.OrdinalIgnoreCase);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<SongHistory> history = JsonSerializer.Deserialize<List<SongHistory>>(body) ?? throw new InvalidOperationException($"{nameof(history)} is null");

        return history;
    }

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    internal static async Task<List<AcListenerModel>> GetListenersAsync(DateTime startTime, DateTime endTime)
    {
        string url = $"{string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.listeners)}?{AcApiEnum.start}={startTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}&{AcApiEnum.end}={endTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}".Replace("_", "T", StringComparison.OrdinalIgnoreCase);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcListenerModel> listenerList = JsonSerializer.Deserialize<List<AcListenerModel>>(body) ?? throw new InvalidOperationException($"{nameof(listenerList)} is null");

        return listenerList;
    }

    private static async Task<List<AcSongRequestsModel>> FindAllMatchingSongsForRequestAsync(string songName, string songArtist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.requests);
        string body = await CoreWebRequests.GetWebAsync(url, null, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcSongRequestsModel> songRequests = JsonSerializer.Deserialize<List<AcSongRequestsModel>>(body) ?? throw new InvalidOperationException("Could not retrieve the list of requestable songs from the server");
        List<AcSongRequestsModel> matchingSongs = [];

        foreach (AcSongRequestsModel songRequest in songRequests)
        {
            SongDetailed song = songRequest.Song;

            // if title is equal
            if (song.Title.Contains(songName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(songArtist))
                {
                    // if title + artist is equal
                    if (song.Artist.StartsWith(songArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingSongs.Clear();
                        matchingSongs.Add(songRequest);
                        break;
                    }
                }

                matchingSongs.Add(songRequest);
            }
        }

        return matchingSongs;
    }

    private static async Task<List<AcSongRequestsModel>> FindAllMatchingSongsForRequestAsync(string songId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songId, nameof(songId));

        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.requests);
        string body = await CoreWebRequests.GetWebAsync(url, null, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcSongRequestsModel> songRequests = JsonSerializer.Deserialize<List<AcSongRequestsModel>>(body) ?? throw new InvalidOperationException("Could not retrieve the list of requestable songs from the server");
        List<AcSongRequestsModel> matchingSongs = [];

        foreach (AcSongRequestsModel songRequest in songRequests)
        {
            SongDetailed song = songRequest.Song;

            if (song.Id == songId)
            {
                matchingSongs.Add(songRequest);
                break;
            }
        }

        return matchingSongs;
    }

    private static async Task<List<AcSongRequestsModel>> FindAllCachedMatchingSongsAsync(string songName, string songArtist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        if (AzuraCastModule.FileCacheLock is null)
            throw new InvalidOperationException($"{nameof(AzuraCastModule.FileCacheLock)} is null");

        string body = await AzuraCastModule.FileCacheLock.GetFileContentAsync();
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcFilesModel> songs = JsonSerializer.Deserialize<List<AcFilesModel>>(body) ?? throw new InvalidOperationException("Could not retrieve the list of requestable songs from the filesystem");
        List<AcSongRequestsModel> matchingSongs = [];

        foreach (AcFilesModel songRequest in songs)
        {
            // if title is equal
            if (songRequest.Title.Contains(songName, StringComparison.OrdinalIgnoreCase))
            {
                AcSongRequestsModel song = new()
                {
                    Song = new()
                    {
                        Title = songRequest.Title,
                        Artist = songRequest.Artist,
                        Album = songRequest.Album,
                        Art = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.art, songRequest.Unique_Id)
                    }
                };

                if (!string.IsNullOrWhiteSpace(songArtist))
                {
                    // if title + artist is equal
                    if (songRequest.Artist.StartsWith(songArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingSongs.Clear();
                        matchingSongs.Add(song);
                        break;
                    }
                }

                matchingSongs.Add(song);
            }
        }

        return matchingSongs;
    }

    internal static async Task<DiscordEmbed> CheckIfSongExistsAsync(string songName, string songArtist, string userName, string userAvatarUrl, bool useOnline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        List<AcSongRequestsModel> matchingSongs = (useOnline) ? await FindAllMatchingSongsForRequestAsync(songName, songArtist) : await FindAllCachedMatchingSongsAsync(songName, songArtist);

        if (matchingSongs.Count == 0)
        {
            // If nothing is equal send message to channel
            await AzzyBot.SendMessageAsync(AcSettings.MusicRequestsChannelId, string.Empty, [AcEmbedBuilder.BuildRequestNotAvailableEmbed(userName, userAvatarUrl, songName, songArtist)]);
        }

        return AcEmbedBuilder.BuildSearchSongRequestsEmbed(userName, userAvatarUrl, matchingSongs);
    }

    internal static async Task<DiscordEmbed> CheckIfSongIsRequestableAsync(string songName, string songArtist, string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        List<AcSongRequestsModel> matchingSongs = await FindAllMatchingSongsForRequestAsync(songName, songArtist);

        // If song is already queued send embed
        // otherwise request song
        SongDetailed song = matchingSongs[0].Song;

        return (await CheckIfSongIsQueuedAsync(song.Id, song.Title))
            ? AcEmbedBuilder.BuildCantRequestThisSong(userName, userAvatarUrl)
            : await RequestSongAsync(userName, userAvatarUrl, matchingSongs[0]);
    }

    private static async Task<DiscordEmbed> RequestSongAsync(string userName, string userAvatarUrl, AcSongRequestsModel songRequest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(songRequest, nameof(songRequest));

        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.request, songRequest.Request_Id);

        // Request the song and save the state inside a variable
        bool isRequested = await CoreWebRequests.PostWebAsync(url, string.Empty, null, AcSettings.Ipv6Available);

        // If song was successfully requested by Azzy send the embed
        // Otherwise send unable to request embed
        return (isRequested)
            ? AcEmbedBuilder.BuildRequestSongEmbed(userName, userAvatarUrl, songRequest)
            : AcEmbedBuilder.BuildCantRequestThisSong(userName, userAvatarUrl);
    }

    internal static async Task<DiscordEmbed> RequestFavouriteSongAsync(DiscordMember requester, DiscordMember favUser)
    {
        ArgumentNullException.ThrowIfNull(requester, nameof(requester));
        ArgumentNullException.ThrowIfNull(favUser, nameof(favUser));

        if (AzuraCastModule.FavoriteSongsLock is null)
            throw new InvalidOperationException($"{nameof(AzuraCastModule.FavoriteSongsLock)} is null");

        string json = await AzuraCastModule.FavoriteSongsLock.GetFileContentAsync();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("json is empty");

        AcFavoriteSongModel favSong = JsonSerializer.Deserialize<AcFavoriteSongModel>(json) ?? throw new InvalidOperationException($"{nameof(favSong)} is null");

        string requestId = string.Empty;
        string songId = string.Empty;
        string songName = string.Empty;
        string songArtist = string.Empty;
        string songAlbum = string.Empty;
        string songArt = string.Empty;

        UserSongList relation = favSong.UserSongList.Find(element => Convert.ToUInt64(element.UserId, CultureInfo.InvariantCulture) == favUser.Id) ?? throw new InvalidOperationException($"{nameof(relation)} is null");

        songId = relation.SongId;
        List<AcSongRequestsModel> favoriteSong = await FindAllMatchingSongsForRequestAsync(songId);

        if (favoriteSong.Count != 1)
            throw new InvalidOperationException("There are more than one favoriteSongs with the same songId");

        requestId = favoriteSong[0].Request_Id;
        songName = favoriteSong[0].Song.Title;
        songArtist = favoriteSong[0].Song.Artist;
        songAlbum = favoriteSong[0].Song.Album;
        songArt = favoriteSong[0].Song.Art;

        if (await CheckIfSongIsQueuedAsync(songId, songName))
            return AcEmbedBuilder.BuildCantRequestThisSong(CoreDiscordChecks.GetBestUsername(requester.Username, requester.Nickname), requester.AvatarUrl);

        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.request, requestId);

        // Request the song and save the state inside a variable
        bool isRequested = await CoreWebRequests.PostWebAsync(url, string.Empty, null, AcSettings.Ipv6Available);
        bool isFavUser = CoreDiscordChecks.CheckUserId(requester.Id, favUser.Id);

        // If song was successfully requested by Azzy send the embed
        // Otherwise send unable to request embed
        return (isRequested)
            ? AcEmbedBuilder.BuildFavouriteSongEmbed(CoreDiscordChecks.GetBestUsername(requester.Username, requester.Nickname), requester.AvatarUrl, songName, songArtist, songAlbum, songArt, isFavUser, favUser.Mention)
            : AcEmbedBuilder.BuildCantRequestThisSong(CoreDiscordChecks.GetBestUsername(requester.Username, requester.Nickname), requester.AvatarUrl);
    }

    internal static async Task<List<AcPlaylistModel>> GetPlaylistsAsync(int playlistId = -1)
    {
        // Value is default value, GET all playlists
        // Otherwise GET only the specific one
        string url = (playlistId == -1)
            ? string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.playlists)
            : string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.playlist, playlistId);

        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        if (body.Contains("You must be logged in to access this page.", StringComparison.OrdinalIgnoreCase))
            return [];

        List<AcPlaylistModel> playlists = [];

        if (playlistId == -1)
        {
            playlists = JsonSerializer.Deserialize<List<AcPlaylistModel>>(body) ?? throw new InvalidOperationException($"{nameof(playlists)} is null");
        }
        else
        {
            AcPlaylistModel playlist = JsonSerializer.Deserialize<AcPlaylistModel>(body) ?? throw new InvalidOperationException($"{nameof(playlist)} is null");

            playlists.Add(playlist);
        }

        return playlists;
    }

    internal static async Task<string> GetSongsFromPlaylistAsync(string playlistName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistName, nameof(playlistName));

        bool playlistExists = false;
        List<AcPlaylistModel> playlists = await GetPlaylistsAsync();
        foreach (AcPlaylistModel playlist in playlists)
        {
            if (playlist.Short_name == playlistName)
            {
                playlistExists = true;
                break;
            }
        }

        if (!playlistExists)
            return string.Empty;

        string url = $"{string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.files, AcApiEnum.list)}?{AcApiEnum.searchPhrase}={AcApiEnum.playlist}:{playlistName}";
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcPlaylistItemModel> songs = JsonSerializer.Deserialize<List<AcPlaylistItemModel>>(body) ?? throw new InvalidOperationException("songs are empty");

        playlistName = playlistName.Replace($"_({nameof(AcPlaylistKeywordsEnum.NOREQUESTS)})", string.Empty, StringComparison.OrdinalIgnoreCase);

        return await CoreFileOperations.CreateTempCsvFileAsync(songs, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{playlistName}.csv");
    }

    internal static async Task<string> GetSongsPlayedAtDateAsync(DateTime dateTime)
    {
        DateTime startDate = dateTime;
        DateTime endDate = dateTime.AddDays(1);

        if (ModuleStates.ClubManagement)
        {
            // Get the opening and closing time of the club by converting the data accordingly
            TimeSpan openingTime = AzuraCastModule.GetClubOpenedTime();
            TimeSpan closingTime = AzuraCastModule.GetClubClosedTime();
            DateTime openTime = new(dateTime.Year, dateTime.Month, dateTime.Day, openingTime.Hours, openingTime.Minutes, openingTime.Seconds);
            DateTime closeTime = new(dateTime.Year, dateTime.Month, dateTime.Day, closingTime.Hours, closingTime.Minutes, closingTime.Seconds);

            // If the closing time is in the next day, then add a day to the closing time
            if (closingTime > new TimeSpan(0, 0, 0) && closingTime < openingTime)
                closeTime = closeTime.AddDays(1);

            startDate = openTime;
            endDate = closeTime;
        }

        List<SongHistory> history = await GetSongHistoryAsync(startDate, endDate);
        List<AcPlaylistModel> playlists = await GetPlaylistsAsync();

        if (history.Count == 0)
            return string.Empty;

        List<SongExportHistory> songs = [];
        foreach (SongHistory item in history)
        {
            SongExportHistory song = new()
            {
                PlayedAt = CoreMisc.ConvertFromUnixTime(item.PlayedAt),
                Song = item.Song,
                Playlist = item.Playlist,
                SongRequest = item.IsRequest,
                Streamer = item.Streamer
            };

            if (item.IsRequest)
            {
                songs.Add(song);
                continue;
            }

            foreach (AcPlaylistModel playlist in playlists)
            {
                if (item.Playlist == playlist.Name && !AzuraCastModule.CheckIfDeniedPlaylist(playlist.Id))
                {
                    songs.Add(song);
                    break;
                }
            }
        }

        // Reverse because history is reversed
        songs.Reverse();

        return (songs.Count == 0)
            ? string.Empty
            : await CoreFileOperations.CreateTempCsvFileAsync(songs, $"{startDate:yyyy-MM-dd}-history.csv");
    }

    internal static async Task TogglePlaylistAsync(int id)
    {
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.playlist, id, AcApiEnum.toggle);

        if (!await CoreWebRequests.PutWebAsync(url, string.Empty, Headers, AcSettings.Ipv6Available))
            throw new InvalidOperationException("Unable to update playlist");
    }

    internal static async Task<string> SwitchPlaylistsAsync(string playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId, nameof(playlistId));

        int id = Convert.ToInt32(playlistId, CultureInfo.InvariantCulture);
        AcPlaylistModel newPlaylist = (await GetPlaylistsAsync(id))[0] ?? throw new InvalidOperationException("Playlist not found!");

        // Get all the playlist names
        List<AcPlaylistModel> playlists = await GetPlaylistsAsync();

        // Check the playlist which is currently active and disable it
        foreach (AcPlaylistModel playlist in playlists)
        {
            if (!AzuraCastModule.CheckIfDeniedPlaylist(playlist.Id) && playlist.Is_enabled)
                await TogglePlaylistAsync(playlist.Id);
        }

        // Switch the playlist
        await TogglePlaylistAsync(id);
        return newPlaylist.Name;
    }

    internal static async Task ChangeSongRequestAvailabilityAsync(bool enabled)
    {
        // Get the station info
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.admin, AcApiEnum.station, AcSettings.AzuraStationKey);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        AcStationConfigModel config = JsonSerializer.Deserialize<AcStationConfigModel>(body) ?? throw new InvalidOperationException($"{nameof(config)} is null");

        config.Enable_requests = enabled;

        string payload = JsonSerializer.Serialize(config, SerializerOptions);

        if (!await CoreWebRequests.PutWebAsync(url, payload, Headers, AcSettings.Ipv6Available))
            throw new InvalidOperationException("Unable to change song request availability");
    }

    internal static async Task CheckIfFilesWereModifiedAsync()
    {
        // If the cache is empty
        bool noCache = false;

        // Get the files from the music server and write into list
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.files);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<AcFilesModel> onlineFiles = JsonSerializer.Deserialize<List<AcFilesModel>>(body) ?? throw new InvalidOperationException($"{nameof(onlineFiles)} is null");

        // Get the local files from the bot and write into list
        if (AzuraCastModule.FileCacheLock is null)
            throw new InvalidOperationException($"{nameof(AzuraCastModule.FileCacheLock)} is null");

        body = await AzuraCastModule.FileCacheLock.GetFileContentAsync();
        if (string.IsNullOrWhiteSpace(body))
            noCache = true;

        List<AcFilesModel> localFiles = (!noCache) ? JsonSerializer.Deserialize<List<AcFilesModel>>(body) ?? throw new InvalidOperationException($"{nameof(localFiles)} is null") : [];

        // Create a HashSet for the online files
        HashSet<AcFilesModel> onlineHashSet = new(onlineFiles, new AcFileComparer());

        // Create a HashSet for the local files
        HashSet<AcFilesModel> localHashSet = new(localFiles, new AcFileComparer());

        List<AcFilesModel> addedFiles = [];
        List<AcFilesModel> removedFiles = [];

        // Check against online files if some file was removed
        foreach (AcFilesModel file in localFiles)
        {
            if (!onlineHashSet.Contains(file))
                removedFiles.Add(file);
        }

        // Check against local files if some file was added
        foreach (AcFilesModel file in onlineFiles)
        {
            if (!localHashSet.Contains(file))
                addedFiles.Add(file);
        }

        // If none were removed nor added - stop
        if (removedFiles.Count == 0 && addedFiles.Count == 0)
            return;

        int numFiles = (addedFiles.Count == 0 || removedFiles.Count == 0) ? 1 : 2;
        int idxFiles = 0;

        string[] files = new string[numFiles];
        string[] fileNames = new string[numFiles];

        string addedFileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-Added.txt";
        string removedFileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-Removed.txt";

        if (addedFiles.Count > 0)
        {
            // Add each file which was added to one big string
            foreach (AcFilesModel file in addedFiles)
            {
                files[idxFiles] += $"{file.Path}\n";
            }

            fileNames[idxFiles] = await CoreFileOperations.CreateTempFileAsync(files[idxFiles], addedFileName);
            idxFiles++;
        }

        if (removedFiles.Count > 0)
        {
            // Add each file which was removed to one big string
            foreach (AcFilesModel file in removedFiles)
            {
                files[idxFiles] += $"{file.Path}\n";
            }

            fileNames[idxFiles] = await CoreFileOperations.CreateTempFileAsync(files[idxFiles], removedFileName);
        }

        await AzzyBot.SendMessageAsync(AcSettings.MusicRequestsChannelId, string.Empty, AcEmbedBuilder.BuildFilesHaveChangedEmbed(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, addedFiles.Count, removedFiles.Count), fileNames);
        if (!await AzuraCastModule.FileCacheLock.SetFileContentAsync(JsonSerializer.Serialize(onlineFiles, SerializerOptions)))
            throw new InvalidOperationException("File couldn't be modified");
    }

    internal static async Task<string> ExportPlaylistsAsFileAsync()
    {
        List<AcPlaylistModel> playlists = await GetPlaylistsAsync();

        string[] directory = [nameof(CoreFileDirectoriesEnum.Modules), nameof(CoreFileDirectoriesEnum.AzuraCast), nameof(CoreFileDirectoriesEnum.Files)];
        if (!CoreFileOperations.CreateDirectory("ZipFile", directory))
            throw new InvalidOperationException("Can't create directory");

        foreach (AcPlaylistModel playlist in playlists)
        {
            if (AzuraCastModule.CheckIfSystemGeneratedPlaylist(playlist.Id))
                continue;

            string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.station, AcSettings.AzuraStationKey, AcApiEnum.playlist, playlist.Id, AcApiEnum.export, AcApiEnum.m3u);
            HttpResponseMessage response = await CoreWebRequests.GetWebDownloadAsync(url, Headers, AcSettings.Ipv6Available);
            Stream stream = await response.Content.ReadAsStreamAsync();

            if (!await CoreFileOperations.WriteFileContentAsync($"{playlist.Short_name}.m3u", directory, "ZipFile", stream))
                throw new InvalidOperationException("File can't be written");

            response.Dispose();
        }

        return CoreFileOperations.CreateZipFile($"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-playlists.zip", directory);
    }

    [SuppressMessage("Roslynator", "RCS1124:Inline local variable", Justification = "Code Style")]
    internal static async Task<AcUpdateModel> CheckIfMusicServerNeedsUpdatesAsync()
    {
        // Get the details from the music server and write into model
        string url = string.Join("/", AcSettings.AzuraApiUrl, AcApiEnum.admin, AcApiEnum.updates);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AcSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        AcUpdateModel model = JsonSerializer.Deserialize<AcUpdateModel>(body) ?? throw new InvalidOperationException($"{nameof(model)} is null");

        return model;
    }
}
