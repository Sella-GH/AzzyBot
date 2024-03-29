using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Structs;
using Newtonsoft.Json;

namespace AzzyBot.Strings.AzuraCast;

internal sealed class AzuraCastStringBuilder : StringBuilding
{
    private static AzuraCastStringModel Model = new();
    private const string RollingUrl = "https://github.com/AzuraCast/AzuraCast/commits/main/";
    private const string StableUrl = "https://github.com/AzuraCast/AzuraCast/commits/stable/";

    internal static async Task<bool> LoadAzuraCastStringsAsync()
    {
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.AzuraCast)];
        string content = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.StringsAzuraCastJSON), directories);

        if (!string.IsNullOrWhiteSpace(content))
        {
            AzuraCastStringModel? newModel = JsonConvert.DeserializeObject<AzuraCastStringModel>(content);
            if (newModel is not null)
            {
                // Reference assignment is atomic in .NET, so this is thread safe.
                Model = newModel;
            }
        }

        return Model is not null;
    }

    #region Commands

    #region CommandsExportPlaylists

    internal static string GetCommandsExportPlaylists => Model.CommandsExportPlaylists;

    #endregion CommandsExportPlaylists

    #region CommandsChangePlaylist

    internal static string GetCommandsChangePlaylist(string playlist) => BuildString(Model.CommandsChangePlaylist, "%PLAYLIST%", playlist);

    #endregion CommandsChangePlaylist

    #region CommandsExportPlaylistContent

    internal static string GetCommandsExportPlaylistContentForgotPlaylist => Model.CommandsExportPlaylistContentForgotPlaylist;
    internal static string GetCommandsExportPlaylistContentNotFound => Model.CommandsExportPlaylistContentNotFound;
    internal static string GetCommandsExportPlaylistContentFound => Model.CommandsExportPlaylistContentFound;

    #endregion CommandsExportPlaylistContent

    #region CommandsGetSongHistory

    internal static string GetCommandsGetSongHistoryForgotDate => Model.CommandsGetSongHistoryForgotDate;
    internal static string GetCommandsGetSongHistoryWrongDate => Model.CommandsGetSongHistoryWrongDate;
    internal static string GetCommandsGetSongHistoryNoOpening => Model.CommandsGetSongHistoryNoOpening;
    internal static string GetCommandsGetSongHistoryTooEarly => Model.CommandsGetSongHistoryTooEarly;
    internal static string GetCommandsGetSongHistoryHistoryFound => Model.CommandsGetSongHistoryHistoryFound;

    #endregion CommandsGetSongHistory

    #region CommandsFindSong

    internal static string GetCommandsFindSongForgotSong => Model.CommandsFindSongForgotSong;

    #endregion CommandsFindSong

    #region CommandsFavoriteSong

    internal static string GetCommandsFavoriteSongForgotUser => Model.CommandsFavoriteSongForgotUser;

    #endregion CommandsFavoriteSong

    #region CommandsForceCacheRefresh

    internal static string GetCommandsForceCacheRefresh => Model.CommandsForceCacheRefresh;

    #endregion CommandsForceCacheRefresh

    #endregion Commands

    #region Embeds

    #region BuildMusicServerStatsEmbed

    internal static string GetEmbedMusicStatsTitle => Model.EmbedMusicStatsTitle;
    internal static string GetEmbedMusicStatsCpuUsageAll(string value) => BuildString(Model.EmbedMusicStatsCpuUsageAll, "%VALUE%", value);
    internal static string GetEmbedMusicStatsCpuUsageCore(int counter, string value) => BuildString(BuildString(Model.EmbedMusicStatsCpuUsageCore, "%COUNTER%", counter), "%VALUE%", value);
    internal static string GetEmbedMusicStatsNetworkTitle(string name) => BuildString(Model.EmbedMusicStatsNetworkUsageTitle, "%NAME%", name);
    internal static string GetEmbedMusicStatsNetworkDesc(double receive, double transmit) => BuildString(BuildString(Model.EmbedMusicStatsNetworkUsageDesc, "%RECEIVE%", receive), "%TRANSMIT%", transmit);

    [SuppressMessage("Roslynator", "RCS1250:Use implicit/explicit object creation", Justification = "Collection Expression are not yet available for Dictionaries.")]
    internal static Dictionary<string, DiscordEmbedStruct> GetEmbedMusicStatsFields(string ping, string coreUsage, double core1Load, double core5Load, double core15Load, double memUsage, double memCached, double memTotalUsed, double memTotal, double diskUsed, double diskTotal)
    {
        return new Dictionary<string, DiscordEmbedStruct>()
        {
            [Model.EmbedMusicStatsPingTitle] = new(nameof(Model.EmbedMusicStatsPingTitle), BuildString(Model.EmbedMusicStatsPingDesc, "%VALUE%", ping), false),
            [Model.EmbedMusicStatsCpuUsageTitle] = new(nameof(Model.EmbedMusicStatsCpuUsageTitle), coreUsage, false),
            [Model.EmbedMusicStats1MinLoadTitle] = new(nameof(Model.EmbedMusicStats1MinLoadTitle), BuildString(Model.EmbedMusicStats1MinLoadDesc, "%VALUE%", core1Load), true),
            [Model.EmbedMusicStats5MinLoadTitle] = new(nameof(Model.EmbedMusicStats5MinLoadTitle), BuildString(Model.EmbedMusicStats5MinLoadDesc, "%VALUE%", core5Load), true),
            [Model.EmbedMusicStats15MinLoadTitle] = new(nameof(Model.EmbedMusicStats15MinLoadTitle), BuildString(Model.EmbedMusicStats15MinLoadDesc, "%VALUE%", core15Load), true),
            [Model.EmbedMusicStatsRamUsageTitle] = new(nameof(Model.EmbedMusicStatsRamUsageTitle), BuildString(BuildString(BuildString(BuildString(Model.EmbedMusicStatsRamUsageDesc, "%USED%", memUsage), "%CACHED%", memCached), "%TUSED%", memTotalUsed), "%TOTAL%", memTotal), false),
            [Model.EmbedMusicStatsDiskUsageTitle] = new(nameof(Model.EmbedMusicStatsDiskUsageTitle), BuildString(BuildString(Model.EmbedMusicStatsDiskUsageDesc, "%USED%", diskUsed), "%TOTAL%", diskTotal), false)
        };
    }

    #endregion BuildMusicServerStatsEmbed

    #region BuildServerIsOfflineEmbed

    internal static string GetEmbedServerStatusOffline => Model.EmbedServerIsOfflineTitle;
    internal static string GetEmbedServerStatusOnline => Model.EmbedServerIsOnlineTitle;

    #endregion BuildServerIsOfflineEmbed

    #region BuildServerNotAvailableEmbed

    internal static string GetEmbedServerNotAvailableTitle => Model.EmbedServerNotAvailableTitle;
    internal static string GetEmbedServerNotAvailableDesc => Model.EmbedServerNotAvailableDesc;

    #endregion BuildServerNotAvailableEmbed

    #region BuildFilesHaveChangedEmbed

    internal static string GetEmbedFilesChangedTitle => Model.EmbedFilesChangedTitle;

    internal static string GetEmbedFilesChangedAdded(int number) => (number == 1) ? Model.EmbedFilesAddedSingle : BuildString(Model.EmbedFilesAddedMulti, "%NUMBER%", number);

    internal static string GetEmbedFilesChangedRemoved(int number) => (number == 1) ? Model.EmbedFilesRemovedSingle : BuildString(Model.EmbedFilesRemovedMulti, "%NUMBER%", number);

    #endregion BuildFilesHaveChangedEmbed

    #region BuildUpdatesAvailableEmbed

    internal static string GetEmbedAzuraUpdateTitle => Model.EmbedAzuraUpdateTitle;
    internal static string GetEmbedAzuraUpdateDesc(int number) => BuildString(Model.EmbedAzuraUpdateDesc, "%NUMBER%", number);
    internal static DiscordEmbedStruct GetEmbedAzuraCurrentRelease(string release) => new(Model.EmbedAzuraUpdateCurrentRelease, release, false);
    internal static DiscordEmbedStruct GetEmbedAzuraLatestRelease(string latest) => new(Model.EmbedAzuraUpdateLatestRelease, latest, false);
    internal static DiscordEmbedStruct GetEmbedAzuraMajorRelease => new(Model.EmbedAzuraUpdateMajorReleaseTitle, Model.EmbedAzuraUpdateMajorReleaseDesc, false);
    internal static DiscordEmbedStruct GetEmbedAzuraSwitch => new(Model.EmbedAzuraUpdateSwitchTitle, Model.EmbedAzuraUpdateSwitchDesc, false);
    internal static string GetEmbedAzuraChangelogPart(int number) => BuildString(Model.EmbedAzuraUpdateChangelogPart, "%NUMBER%", number);
    internal static DiscordEmbedStruct GetEmbedAzuraChangelog(int number, string changelog) => new((number == 1) ? Model.EmbedAzuraUpdateChangelog : Model.EmbedAzuraUpdateChangelogPart, changelog, false);
    internal static DiscordEmbedStruct GetEmbedAzuraTooBig(bool rolling) => new(Model.EmbedAzuraUpdateChangelog, BuildString(Model.EmbedAzuraUpdatesTooBig, "%URL%", (rolling) ? RollingUrl : StableUrl), false);

    #endregion BuildUpdatesAvailableEmbed

    #region BuildPlaylistChangesNotAllowedEmbed

    internal static string GetEmbedAzuraPlaylistChangesTitle => Model.EmbedAzuraPlaylistChangesTitle;
    internal static string GetEmbedAzuraPlaylistChangesDesc => Model.EmbedAzuraPlaylistChangesDesc;

    #endregion BuildPlaylistChangesNotAllowedEmbed

    #region BuildSongRequestsNotAllowedEmbed

    internal static string EmbedAzuraSongRequestsTitle => Model.EmbedAzuraSongRequestsTitle;
    internal static string EmbedAzuraSongRequestsDesc => Model.EmbedAzuraSongRequestsDesc;

    #endregion BuildSongRequestsNotAllowedEmbed

    #region BuildCantRequestThisSong

    internal static string EmbedAzuraSongInQueueTitle => Model.EmbedAzuraSongInQueueTitle;
    internal static string EmbedAzuraSongInQueueDesc => Model.EmbedAzuraSongInQueueDesc;

    #endregion BuildCantRequestThisSong

    #region BuildNowPlayingEmbed

    internal static string GetEmbedAzuraNowPlayingTitle => Model.EmbedAzuraNowPlayingTitle;
    internal static DiscordEmbedStruct GetEmbedAzuraNowPlayingSong(string song) => new(Model.EmbedAzuraNowPlayingSong, song, true);
    internal static DiscordEmbedStruct GetEmbedAzuraNowPlayingArtist(string artist) => new(Model.EmbedAzuraNowPlayingArtist, artist, true);
    internal static DiscordEmbedStruct GetEmbedAzuraNowPlayingAlbum(string album) => new(Model.EmbedAzuraNowPlayingAlbum, album, true);
    internal static string GetEmbedAzuraNowPlayingStreamMessage(string name) => BuildString(Model.EmbedAzuraNowPlayingStreamMessage, "%NAME%", name);
    internal static DiscordEmbedStruct GetEmbedAzuraNowPlayingStreamSince(int date) => new(Model.EmbedAzuraNowPlayingStreamSince, $"<t:{date}>", false);
    internal static DiscordEmbedStruct GetEmbedAzuraNowPlayingDuration(string duration, string songElapsed, string songDuration) => new(Model.EmbedAzuraNowPlayingDuration, $"{duration} `[{songElapsed} / {songDuration}]`", false);
    internal static string GetEmbedAzuraNowPlayingFooterClosed => Model.EmbedAzuraNowPlayingFooterClosed;
    internal static DiscordEmbedStruct GetEmbedAzuraNowPlayingPlaylist(string playlist) => new(Model.EmbedAzuraNowPlayingPlaylist, playlist, false);
    internal static DiscordEmbedStruct GetEmbedAzuraNowPlayingSlogan(string slogan) => new(Model.EmbedAzuraNowPlayingSlogan, slogan, false);

    #endregion BuildNowPlayingEmbed

    #region BuildSearchSongRequestsEmbed

    internal static string GetEmbedAzuraSearchSongRequestsTitle => Model.EmbedAzuraSearchSongRequestsTitle;
    internal static string GetEmbedAzuraSearchSongRequestsAvaDesc => Model.EmbedAzuraSearchSongRequestsAvaDesc;
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsAvaSong(string song) => new(Model.EmbedAzuraSearchSongRequestsAvaSong, song, true);
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsAvaArtist(string artist) => new(Model.EmbedAzuraSearchSongRequestsAvaArtist, artist, true);
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsAvaAlbum(string album) => new(Model.EmbedAzuraSearchSongRequestsAvaAlbum, (string.IsNullOrWhiteSpace(album)) ? Model.EmbedAzuraSearchSongRequestsFoundAlbumNotAvailable : album, true);
    internal static string GetEmbedAzuraSearchSongRequestsFoundDesc => Model.EmbedAzuraSearchSongRequestsFoundDesc;

    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsFoundInfo(int counter, string song, string artist, string album = "")
    {
        string text = $"**{song}**\n{Model.EmbedAzuraSearchSongRequestsFoundArtist} **{artist}**";
        text += $"\n{Model.EmbedAzuraSearchSongRequestsFoundAlbum} **{((string.IsNullOrWhiteSpace(album)) ? Model.EmbedAzuraSearchSongRequestsFoundAlbumNotAvailable : album)}**";

        return new($"{Model.EmbedAzuraSearchSongRequestsFoundSong} {counter}", text, false);
    }

    internal static string GetEmbedAzuraSearchSongRequestsFoundFooter => Model.EmbedAzuraSearchSongRequestsFoundFooter;
    internal static string GetEmbedAzuraSearchSongRequestsNotDesc => Model.EmbedAzuraSearchSongRequestsNotDesc;
    internal static string GetEmbedAzuraSearchSongRequestsNotFooter => Model.EmbedAzuraSearchSongRequestsNotFooter;

    #endregion BuildSearchSongRequestsEmbed

    #region BuildRequestNotAvailableEmbed

    internal static string GetEmbedAzuraSearchSongRequestsNotAvaTitle => Model.EmbedAzuraSearchSongRequestsNotAvaTitle;
    internal static string GetEmbedAzuraSearchSongRequestsNotAvaDesc(string name) => BuildString(Model.EmbedAzuraSearchSongRequestsNotAvaDesc, "%NAME%", name);
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsNotAvaSong(string song) => new(Model.EmbedAzuraSearchSongRequestsAvaSong, song, false);
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsNotAvaArtist(string artist) => new(Model.EmbedAzuraSearchSongRequestsAvaArtist, artist, false);

    #endregion BuildRequestNotAvailableEmbed

    #region BuildRequestSongEmbed

    internal static string GetEmbedAzuraSearchSongRequestsRequestTitle => Model.EmbedAzuraSearchSongRequestsRequestTitle;
    internal static string GetEmbedAzuraSearchSongRequestsRequestDesc => Model.EmbedAzuraSearchSongRequestsRequestDesc;
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsRequestSong(string song) => new(Model.EmbedAzuraSearchSongRequestsRequestSong, song, true);
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsRequestArtist(string artist) => new(Model.EmbedAzuraSearchSongRequestsRequestArtist, artist, true);
    internal static DiscordEmbedStruct GetEmbedAzuraSearchSongRequestsRequestAlbum(string album) => new(Model.EmbedAzuraSearchSongRequestsRequestAlbum, album, true);

    #endregion BuildRequestSongEmbed

    #region BuildFavouriteSongEmbed

    internal static string GetEmbedAzuraFavoriteSongTitle(string name) => BuildString(Model.EmbedAzuraFavoriteSongTitle, "%NAME%", name);
    internal static string GetEmbedAzuraFavoriteSongDescUser(bool isUser, string name) => (isUser) ? Model.EmbedAzuraFavoriteSongDescIsUser : BuildString(Model.EmbedAzuraFavoriteSongDescIsNot, "%NAME%", name);
    internal static DiscordEmbedStruct GetEmbedAzuraFavoriteSongSong(string song) => new(Model.EmbedAzuraSearchSongRequestsRequestDesc, song, true);
    internal static DiscordEmbedStruct GetEmbedAzuraFavoriteSongArtist(string artist) => new(Model.EmbedAzuraSearchSongRequestsRequestArtist, artist, true);
    internal static DiscordEmbedStruct GetEmbedAzuraFavoriteSongAlbum(string album) => new(Model.EmbedAzuraSearchSongRequestsRequestAlbum, album, true);

    #endregion BuildFavouriteSongEmbed

    #endregion Embeds
}
