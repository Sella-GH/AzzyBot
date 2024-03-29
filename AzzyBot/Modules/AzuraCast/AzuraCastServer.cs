using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Structs;
using AzzyBot.Settings.AzuraCast;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast;

internal static class AzuraCastServer
{
    internal static readonly Dictionary<string, string> Headers = new()
    {
        ["accept"] = "application/json",
        ["X-API-Key"] = AzuraCastSettings.AzuraApiKey
    };

    internal static async Task<NowPlayingData> GetNowPlayingAsync()
    {
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.nowplaying, AzuraCastSettings.AzuraStationKey);
        string body = await CoreWebRequests.GetWebAsync(url, null, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        NowPlayingData? data = JsonConvert.DeserializeObject<NowPlayingData>(body);

        if (data is null)
            throw new InvalidOperationException($"{nameof(data)} is null");

        return data;
    }

    internal static async Task<PlaylistSloganStruct> GetCurrentSloganAsync(NowPlayingData nowPlaying)
    {
        ArgumentNullException.ThrowIfNull(nowPlaying, nameof(nowPlaying));

        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.AzuraCast)];
        string configBody = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.PlaylistSlogansJSON), directories);
        if (string.IsNullOrWhiteSpace(configBody))
            throw new InvalidOperationException("configBody is empty");

        ConfigSlogansModel? playlistSlogan = JsonConvert.DeserializeObject<ConfigSlogansModel>(configBody);

        if (playlistSlogan is null)
            throw new InvalidOperationException($"{nameof(playlistSlogan)} & {nameof(nowPlaying)} are null");

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
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.admin, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        StationConfigModel? config = JsonConvert.DeserializeObject<StationConfigModel>(body);

        if (config is null)
            throw new InvalidOperationException($"{nameof(config)} is null");

        if (!config.Enable_requests)
            return false;

        NowPlayingData nowPlaying = await GetNowPlayingAsync();
        string playlist = nowPlaying.Now_Playing.Playlist;

        return !playlist.Contains(nameof(AzuraCastPlaylistKeywordsEnum.NOREQUESTS), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> CheckIfSongIsQueuedAsync(string id, string songName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        // Get the song request queue first
        List<SongRequestsQueueModel> requestQueue = await GetSongRequestsQueuesAsync();

        // Then get the regular queue after
        List<QueueItemModel> queue = await GetQueueAsync();

        // Lastly get the song request history
        List<SongRequestsQueueModel> history = await GetSongRequestsHistoryAsync(songName);

        // First check the regular queue if the song is there
        foreach (QueueItemModel item in queue)
        {
            if (item.Song.Id == id)
                return true;
        }

        // Then check the song request history if the song was requested in the last 10 minutes
        // The 10 minute stuff comes from AzuraCast, can't change it
        foreach (SongRequestsQueueModel historyItem in history)
        {
            if (historyItem.Track.Song_Id == id)
            {
                long diff = CoreMisc.ConvertToUnixTime(DateTime.Now) - historyItem.Timestamp;

                if (diff <= 600)
                    return true;
            }
        }

        // Lastly check the song request queue if the song is there
        foreach (SongRequestsQueueModel requestItem in requestQueue)
        {
            if (requestItem.Track.Song_Id == id)
                return true;
        }

        return false;
    }

    private static async Task<List<SongRequestsQueueModel>> GetSongRequestsQueuesAsync()
    {
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.reports, AzuraCastApiEnum.requests);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<SongRequestsQueueModel>? requestQueue = JsonConvert.DeserializeObject<List<SongRequestsQueueModel>>(body);

        if (requestQueue is null)
            throw new InvalidOperationException($"{nameof(requestQueue)} is null");

        return requestQueue;
    }

    private static async Task<List<QueueItemModel>> GetQueueAsync()
    {
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.queue);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<QueueItemModel>? queue = JsonConvert.DeserializeObject<List<QueueItemModel>>(body);

        if (queue is null)
            throw new InvalidOperationException($"{nameof(queue)} is null");

        return queue;
    }

    private static async Task<List<SongRequestsQueueModel>> GetSongRequestsHistoryAsync(string songName = "")
    {
        string url = $"{string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.reports, AzuraCastApiEnum.requests)}?{AzuraCastApiEnum.type}={AzuraCastApiEnum.history}&{AzuraCastApiEnum.searchPhrase}={songName.Replace(" ", "+", StringComparison.InvariantCultureIgnoreCase)}";
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<SongRequestsQueueModel>? history = JsonConvert.DeserializeObject<List<SongRequestsQueueModel>>(body);

        if (history is null)
            throw new InvalidOperationException($"{nameof(history)} is null");

        return history;
    }

    internal static async Task<List<SongHistory>> GetSongHistoryAsync(DateTime startTime, DateTime endTime)
    {
        string url = $"{string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.history)}?{AzuraCastApiEnum.start}={startTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}&{AzuraCastApiEnum.end}={endTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}".Replace("_", "T", StringComparison.OrdinalIgnoreCase);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<SongHistory>? history = JsonConvert.DeserializeObject<List<SongHistory>>(body);

        if (history is null)
            throw new InvalidOperationException($"{nameof(history)} is null");

        return history;
    }

    internal static async Task<List<ListenerModel>> GetListenersAsync(DateTime startTime, DateTime endTime)
    {
        string url = $"{string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.listeners)}?{AzuraCastApiEnum.start}={startTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}&{AzuraCastApiEnum.end}={endTime.ToString("yyyy-MM-dd_HH:mm:ss.fff", CultureInfo.InvariantCulture)}".Replace("_", "T", StringComparison.OrdinalIgnoreCase);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<ListenerModel>? listenerList = JsonConvert.DeserializeObject<List<ListenerModel>>(body);

        if (listenerList is null)
            throw new InvalidOperationException($"{nameof(listenerList)} is null");

        return listenerList;
    }

    private static async Task<List<SongRequestsModel>> FindAllMatchingSongsForRequestAsync(string songName, string songArtist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.requests);
        string body = await CoreWebRequests.GetWebAsync(url, null, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<SongRequestsModel>? songRequests = JsonConvert.DeserializeObject<List<SongRequestsModel>>(body);
        List<SongRequestsModel> matchingSongs = [];

        if (songRequests is null)
            throw new InvalidOperationException("Could not retrieve the list of requestable songs from the server");

        foreach (SongRequestsModel songRequest in songRequests)
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

    private static async Task<List<SongRequestsModel>> FindAllMatchingSongsForRequestAsync(string songId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songId, nameof(songId));

        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.requests);
        string body = await CoreWebRequests.GetWebAsync(url, null, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<SongRequestsModel>? songRequests = JsonConvert.DeserializeObject<List<SongRequestsModel>>(body);
        List<SongRequestsModel> matchingSongs = [];

        if (songRequests is null)
            throw new InvalidOperationException("Could not retrieve the list of requestable songs from the server");

        foreach (SongRequestsModel songRequest in songRequests)
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

    private static async Task<List<SongRequestsModel>> FindAllCachedMatchingSongsAsync(string songName, string songArtist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));

        if (AzuraCastModule.FileCacheLock is null)
            throw new InvalidOperationException($"{nameof(AzuraCastModule.FileCacheLock)} is null");

        string body = await AzuraCastModule.FileCacheLock.GetFileContentAsync();
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<FilesModel>? songs = JsonConvert.DeserializeObject<List<FilesModel>>(body);
        List<SongRequestsModel> matchingSongs = [];

        if (songs is null)
            throw new InvalidOperationException("Could not retrieve the list of requestable songs from the filesystem");

        foreach (FilesModel songRequest in songs)
        {
            // if title is equal
            if (songRequest.Title.Contains(songName, StringComparison.OrdinalIgnoreCase))
            {
                SongRequestsModel song = new()
                {
                    Song = new()
                    {
                        Title = songRequest.Title,
                        Artist = songRequest.Artist,
                        Album = songRequest.Album,
                        Art = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.art, songRequest.Unique_Id)
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

        List<SongRequestsModel> matchingSongs = (useOnline) ? await FindAllMatchingSongsForRequestAsync(songName, songArtist) : await FindAllCachedMatchingSongsAsync(songName, songArtist);

        if (matchingSongs.Count == 0)
        {
            //
            // If nothing is equal
            // Send message to channel
            //
            await Program.SendMessageAsync(AzuraCastSettings.MusicRequestsChannelId, string.Empty, AzuraCastEmbedBuilder.BuildRequestNotAvailableEmbed(userName, userAvatarUrl, songName, songArtist));
        }

        return AzuraCastEmbedBuilder.BuildSearchSongRequestsEmbed(userName, userAvatarUrl, matchingSongs);
    }

    internal static async Task<DiscordEmbed> CheckIfSongIsRequestableAsync(string songName, string songArtist, string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songName, nameof(songName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        List<SongRequestsModel> matchingSongs = await FindAllMatchingSongsForRequestAsync(songName, songArtist);

        // If song is already queued send embed
        // otherwise request song
        SongDetailed song = matchingSongs[0].Song;

        return (await CheckIfSongIsQueuedAsync(song.Id, song.Title))
            ? AzuraCastEmbedBuilder.BuildCantRequestThisSong(userName, userAvatarUrl)
            : await RequestSongAsync(userName, userAvatarUrl, matchingSongs[0]);
    }

    private static async Task<DiscordEmbed> RequestSongAsync(string userName, string userAvatarUrl, SongRequestsModel songRequest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(songRequest, nameof(songRequest));

        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.request, songRequest.Request_Id);

        // Request the song and save the state inside a variable
        bool isRequested = await CoreWebRequests.PostWebAsync(url, string.Empty, null, AzuraCastSettings.Ipv6Available);

        // If song was successfully requested by Azzy send the embed
        // Otherwise send unable to request embed
        return (isRequested)
            ? AzuraCastEmbedBuilder.BuildRequestSongEmbed(userName, userAvatarUrl, songRequest)
            : AzuraCastEmbedBuilder.BuildCantRequestThisSong(userName, userAvatarUrl);
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

        FavoriteSongModel? favSong = JsonConvert.DeserializeObject<FavoriteSongModel>(json);

        if (favSong is null)
            throw new InvalidOperationException($"{nameof(favSong)} is null");

        string requestId = string.Empty;
        string songId = string.Empty;
        string songName = string.Empty;
        string songArtist = string.Empty;
        string songAlbum = string.Empty;
        string songArt = string.Empty;

        UserSongList? relation = favSong.UserSongList.Find(element => Convert.ToUInt64(element.UserId, CultureInfo.InvariantCulture) == favUser.Id);

        if (relation is null)
            throw new InvalidOperationException($"{nameof(relation)} is null");

        songId = relation.SongId;
        List<SongRequestsModel> favoriteSong = await FindAllMatchingSongsForRequestAsync(songId);

        if (favoriteSong.Count != 1)
            throw new InvalidOperationException("There are more than one favoriteSongs with the same songId");

        requestId = favoriteSong[0].Request_Id;
        songName = favoriteSong[0].Song.Title;
        songArtist = favoriteSong[0].Song.Artist;
        songAlbum = favoriteSong[0].Song.Album;
        songArt = favoriteSong[0].Song.Art;

        if (await CheckIfSongIsQueuedAsync(songId, songName))
            return AzuraCastEmbedBuilder.BuildCantRequestThisSong(CoreDiscordCommands.GetBestUsername(requester.Username, requester.Nickname), requester.AvatarUrl);

        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.request, requestId);

        // Request the song and save the state inside a variable
        bool isRequested = await CoreWebRequests.PostWebAsync(url, string.Empty, null, AzuraCastSettings.Ipv6Available);
        bool isFavUser = CoreDiscordCommands.CheckUserId(requester.Id, favUser.Id);

        // If song was successfully requested by Azzy send the embed
        // Otherwise send unable to request embed
        return (isRequested)
            ? AzuraCastEmbedBuilder.BuildFavouriteSongEmbed(CoreDiscordCommands.GetBestUsername(requester.Username, requester.Nickname), requester.AvatarUrl, songName, songArtist, songAlbum, songArt, isFavUser, favUser.Mention)
            : AzuraCastEmbedBuilder.BuildCantRequestThisSong(CoreDiscordCommands.GetBestUsername(requester.Username, requester.Nickname), requester.AvatarUrl);
    }

    internal static async Task<List<PlaylistModel>> GetPlaylistsAsync(int playlistId = -1)
    {
        // Value is default value, GET all playlists
        // Otherwise GET only the specific one
        string url = (playlistId == -1)
            ? string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.playlists)
            : string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.playlist, playlistId);

        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        if (body.Contains("You must be logged in to access this page.", StringComparison.OrdinalIgnoreCase))
            return [];

        List<PlaylistModel>? playlists = [];

        if (playlistId == -1)
        {
            playlists = JsonConvert.DeserializeObject<List<PlaylistModel>>(body);
        }
        else
        {
            PlaylistModel? playlist = JsonConvert.DeserializeObject<PlaylistModel>(body);

            if (playlist is null)
                throw new InvalidOperationException($"{nameof(playlist)} is null");

            playlists.Add(playlist);
        }

        if (playlists is null)
            throw new InvalidOperationException($"{nameof(playlists)} is null");

        return playlists;
    }

    internal static async Task<string> GetSongsFromPlaylistAsync(string playlistName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistName, nameof(playlistName));

        bool playlistExists = false;
        List<PlaylistModel> playlists = await GetPlaylistsAsync();
        foreach (PlaylistModel playlist in playlists)
        {
            if (playlist.Short_name == playlistName)
            {
                playlistExists = true;
                break;
            }
        }

        if (!playlistExists)
            return string.Empty;

        string url = $"{string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.files, AzuraCastApiEnum.list)}?{AzuraCastApiEnum.searchPhrase}={AzuraCastApiEnum.playlist}:{playlistName}";
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<PlaylistItemModel>? songs = JsonConvert.DeserializeObject<List<PlaylistItemModel>>(body);

        playlistName = playlistName.Replace($"_({nameof(AzuraCastPlaylistKeywordsEnum.NOREQUESTS)})", string.Empty, StringComparison.OrdinalIgnoreCase);

        return (songs is null)
            ? throw new InvalidOperationException("songs are empty")
            : await CoreFileOperations.CreateTempCsvFileAsync(songs, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{playlistName}.csv");
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
        List<PlaylistModel> playlists = await GetPlaylistsAsync();

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

            foreach (PlaylistModel playlist in playlists)
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
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.playlist, id, AzuraCastApiEnum.toggle);

        if (!await CoreWebRequests.PutWebAsync(url, string.Empty, Headers, AzuraCastSettings.Ipv6Available))
            throw new InvalidOperationException("Unable to update playlist");
    }

    internal static async Task<string> SwitchPlaylistsAsync(string playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId, nameof(playlistId));

        int id = Convert.ToInt32(playlistId, CultureInfo.InvariantCulture);
        List<PlaylistModel> newPlaylist = await GetPlaylistsAsync(id);

        if (newPlaylist.Count != 1)
            throw new InvalidOperationException("There are more playlists than one");

        // Get all the playlist names
        List<PlaylistModel> playlists = await GetPlaylistsAsync();

        // Check the playlist which is currently active and disable it
        foreach (PlaylistModel playlist in playlists)
        {
            if (!AzuraCastModule.CheckIfDeniedPlaylist(playlist.Id) && playlist.Is_enabled)
                await TogglePlaylistAsync(playlist.Id);
        }

        // Switch the playlist
        await TogglePlaylistAsync(id);
        return newPlaylist[0].Name;
    }

    internal static async Task ChangeSongRequestAvailabilityAsync(bool enabled)
    {
        // Get the station info
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.admin, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        StationConfigModel? config = JsonConvert.DeserializeObject<StationConfigModel>(body);

        if (config is null)
            throw new InvalidOperationException($"{nameof(config)} is null");

        config.Enable_requests = enabled;

        string payload = JsonConvert.SerializeObject(config, Formatting.Indented);

        if (!await CoreWebRequests.PutWebAsync(url, payload, Headers, AzuraCastSettings.Ipv6Available))
            throw new InvalidOperationException("Unable to change song request availability");
    }

    internal static async Task CheckIfFilesWereModifiedAsync()
    {
        // If the cache is empty
        bool noCache = false;

        // Get the files from the music server and write into list
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.files);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        List<FilesModel>? onlineFiles = JsonConvert.DeserializeObject<List<FilesModel>>(body);

        // Get the local files from the bot and write into list
        if (AzuraCastModule.FileCacheLock is null)
            throw new InvalidOperationException($"{nameof(AzuraCastModule.FileCacheLock)} is null");

        body = await AzuraCastModule.FileCacheLock.GetFileContentAsync();
        if (string.IsNullOrWhiteSpace(body))
            noCache = true;

        List<FilesModel>? localFiles = (!noCache) ? JsonConvert.DeserializeObject<List<FilesModel>>(body) : [];

        // Create a HashSet for the online files
        HashSet<FilesModel> onlineHashSet;
        if (onlineFiles is null)
            throw new InvalidOperationException($"{nameof(onlineFiles)} is null");

        onlineHashSet = new(onlineFiles, new AzuraCastFileComparer());

        // Create a HashSet for the local files
        HashSet<FilesModel> localHashSet;
        if (localFiles is null)
            throw new InvalidOperationException($"{nameof(localFiles)} is null");

        localHashSet = new(localFiles, new AzuraCastFileComparer());

        List<FilesModel> addedFiles = [];
        List<FilesModel> removedFiles = [];

        // Check against online files if some file was removed
        foreach (FilesModel file in localFiles)
        {
            if (!onlineHashSet.Contains(file))
                removedFiles.Add(file);
        }

        // Check against local files if some file was added
        foreach (FilesModel file in onlineFiles)
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
            foreach (FilesModel file in addedFiles)
            {
                files[idxFiles] += $"{file.Path}\n";
            }

            fileNames[idxFiles] = await CoreFileOperations.CreateTempFileAsync(files[idxFiles], addedFileName);
            idxFiles++;
        }

        if (removedFiles.Count > 0)
        {
            // Add each file which was removed to one big string
            foreach (FilesModel file in removedFiles)
            {
                files[idxFiles] += $"{file.Path}\n";
            }

            fileNames[idxFiles] = await CoreFileOperations.CreateTempFileAsync(files[idxFiles], removedFileName);
        }

        await Program.SendMessageAsync(AzuraCastSettings.MusicRequestsChannelId, string.Empty, AzuraCastEmbedBuilder.BuildFilesHaveChangedEmbed(Program.GetDiscordClientUserName, Program.GetDiscordClientAvatarUrl, addedFiles.Count, removedFiles.Count), fileNames);
        if (!await AzuraCastModule.FileCacheLock.SetFileContentAsync(JsonConvert.SerializeObject(onlineFiles, Formatting.Indented)))
            throw new InvalidOperationException("File couldn't be modified");
    }

    internal static async Task<string> ExportPlaylistsAsFileAsync()
    {
        List<PlaylistModel> playlists = await GetPlaylistsAsync();

        string[] directory = [nameof(CoreFileDirectoriesEnum.Modules), nameof(CoreFileDirectoriesEnum.AzuraCast), nameof(CoreFileDirectoriesEnum)];
        if (!CoreFileOperations.CreateDirectory("ZipFile", directory))
            throw new InvalidOperationException("Can't create directory");

        foreach (PlaylistModel playlist in playlists)
        {
            if (AzuraCastModule.CheckIfSystemGeneratedPlaylist(playlist.Id))
                continue;

            string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.station, AzuraCastSettings.AzuraStationKey, AzuraCastApiEnum.playlist, playlist.Id, AzuraCastApiEnum.export, AzuraCastApiEnum.m3u);
            Stream stream = await CoreWebRequests.GetWebDownloadAsync(url, Headers, AzuraCastSettings.Ipv6Available);
            await using (stream)
            {
                if (!await CoreFileOperations.WriteFileContentAsync($"{playlist.Short_name}.m3u", directory, "ZipFile", stream))
                    throw new InvalidOperationException("File can't be written");
            }
        }

        return CoreFileOperations.CreateZipFile($"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-playlists.zip", directory);
    }

    internal static async Task<AzuraCastUpdateModel> CheckIfMusicServerNeedsUpdatesAsync()
    {
        // Get the details from the music server and write into model
        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl, AzuraCastApiEnum.admin, AzuraCastApiEnum.updates);
        string body = await CoreWebRequests.GetWebAsync(url, Headers, AzuraCastSettings.Ipv6Available);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("body is empty");

        AzuraCastUpdateModel? model = JsonConvert.DeserializeObject<AzuraCastUpdateModel>(body);

        if (model is not null)
            return model;

        throw new InvalidOperationException($"{nameof(model)} is null");
    }
}
