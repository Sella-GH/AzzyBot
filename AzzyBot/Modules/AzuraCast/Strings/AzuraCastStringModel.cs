namespace AzzyBot.Modules.AzuraCast.Strings;

internal sealed class AzuraCastStringModel
{
    #region Commands

    #region CommandsExportPlaylists

    public string CommandsExportPlaylists { get; set; } = string.Empty;

    #endregion CommandsExportPlaylists

    #region CommandsChangePlaylist

    public string CommandsChangePlaylist { get; set; } = string.Empty;

    #endregion CommandsChangePlaylist

    #region CommandsExportPlaylistContent

    public string CommandsExportPlaylistContentForgotPlaylist { get; set; } = string.Empty;
    public string CommandsExportPlaylistContentNotFound { get; set; } = string.Empty;
    public string CommandsExportPlaylistContentFound { get; set; } = string.Empty;

    #endregion CommandsExportPlaylistContent

    #region CommandsGetSongHistory

    public string CommandsGetSongHistoryForgotDate { get; set; } = string.Empty;
    public string CommandsGetSongHistoryWrongDate { get; set; } = string.Empty;
    public string CommandsGetSongHistoryNoOpening { get; set; } = string.Empty;
    public string CommandsGetSongHistoryTooEarly { get; set; } = string.Empty;
    public string CommandsGetSongHistoryHistoryFound { get; set; } = string.Empty;

    #endregion CommandsGetSongHistory

    #region CommandsFindSong

    public string CommandsFindSongForgotSong { get; set; } = string.Empty;

    #endregion CommandsFindSong

    #region CommandsFavoriteSong

    public string CommandsFavoriteSongForgotUser { get; set; } = string.Empty;

    #endregion CommandsFavoriteSong

    #region CommandsForceCacheRefresh

    public string CommandsForceCacheRefresh { get; set; } = string.Empty;

    #endregion CommandsForceCacheRefresh

    #endregion Commands

    #region Embeds

    #region BuildMusicServerStatsEmbed

    public string EmbedMusicStatsTitle { get; set; } = string.Empty;
    public string EmbedMusicStatsPingTitle { get; set; } = string.Empty;
    public string EmbedMusicStatsPingDesc { get; set; } = string.Empty;
    public string EmbedMusicStatsCpuUsageTitle { get; set; } = string.Empty;
    public string EmbedMusicStatsCpuUsageAll { get; set; } = string.Empty;
    public string EmbedMusicStatsCpuUsageCore { get; set; } = string.Empty;
    public string EmbedMusicStats1MinLoadTitle { get; set; } = string.Empty;
    public string EmbedMusicStats1MinLoadDesc { get; set; } = string.Empty;
    public string EmbedMusicStats5MinLoadTitle { get; set; } = string.Empty;
    public string EmbedMusicStats5MinLoadDesc { get; set; } = string.Empty;
    public string EmbedMusicStats15MinLoadTitle { get; set; } = string.Empty;
    public string EmbedMusicStats15MinLoadDesc { get; set; } = string.Empty;
    public string EmbedMusicStatsRamUsageTitle { get; set; } = string.Empty;
    public string EmbedMusicStatsRamUsageDesc { get; set; } = string.Empty;
    public string EmbedMusicStatsDiskUsageTitle { get; set; } = string.Empty;
    public string EmbedMusicStatsDiskUsageDesc { get; set; } = string.Empty;
    public string EmbedMusicStatsNetworkUsageTitle { get; set; } = string.Empty;
    public string EmbedMusicStatsNetworkUsageDesc { get; set; } = string.Empty;

    #endregion BuildMusicServerStatsEmbed

    #region BuildServerIsOfflineEmbed

    public string EmbedServerIsOfflineTitle { get; set; } = string.Empty;
    public string EmbedServerIsOnlineTitle { get; set; } = string.Empty;

    #endregion BuildServerIsOfflineEmbed

    #region BuildServerNotAvailableEmbed

    public string EmbedServerNotAvailableTitle { get; set; } = string.Empty;
    public string EmbedServerNotAvailableDesc { get; set; } = string.Empty;

    #endregion BuildServerNotAvailableEmbed

    #region BuildFilesHaveChangedEmbed

    public string EmbedFilesChangedTitle { get; set; } = string.Empty;
    public string EmbedFilesAddedSingle { get; set; } = string.Empty;
    public string EmbedFilesAddedMulti { get; set; } = string.Empty;
    public string EmbedFilesRemovedSingle { get; set; } = string.Empty;
    public string EmbedFilesRemovedMulti { get; set; } = string.Empty;

    #endregion BuildFilesHaveChangedEmbed

    #region BuildUpdatesAvailableEmbed

    public string EmbedAzuraUpdateTitle { get; set; } = string.Empty;
    public string EmbedAzuraUpdateDesc { get; set; } = string.Empty;
    public string EmbedAzuraUpdateCurrentRelease { get; set; } = string.Empty;
    public string EmbedAzuraUpdateLatestRelease { get; set; } = string.Empty;
    public string EmbedAzuraUpdateMajorReleaseTitle { get; set; } = string.Empty;
    public string EmbedAzuraUpdateMajorReleaseDesc { get; set; } = string.Empty;
    public string EmbedAzuraUpdateSwitchTitle { get; set; } = string.Empty;
    public string EmbedAzuraUpdateSwitchDesc { get; set; } = string.Empty;
    public string EmbedAzuraUpdatesTooBig { get; set; } = string.Empty;
    public string EmbedAzuraUpdateChangelogPart { get; set; } = string.Empty;
    public string EmbedAzuraUpdateChangelog { get; set; } = string.Empty;

    #endregion BuildUpdatesAvailableEmbed

    #region BuildPlaylistChangesNotAllowedEmbed

    public string EmbedAzuraPlaylistChangesTitle { get; set; } = string.Empty;
    public string EmbedAzuraPlaylistChangesDesc { get; set; } = string.Empty;

    #endregion BuildPlaylistChangesNotAllowedEmbed

    #region BuildSongRequestsNotAllowedEmbed

    public string EmbedAzuraSongRequestsTitle { get; set; } = string.Empty;
    public string EmbedAzuraSongRequestsDesc { get; set; } = string.Empty;

    #endregion BuildSongRequestsNotAllowedEmbed

    #region BuildCantRequestThisSong

    public string EmbedAzuraSongInQueueTitle { get; set; } = string.Empty;
    public string EmbedAzuraSongInQueueDesc { get; set; } = string.Empty;

    #endregion BuildCantRequestThisSong

    #region BuildNowPlayingEmbed

    public string EmbedAzuraNowPlayingTitle { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingSong { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingArtist { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingAlbum { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingStreamMessage { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingStreamSince { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingDuration { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingFooterClosed { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingPlaylist { get; set; } = string.Empty;
    public string EmbedAzuraNowPlayingSlogan { get; set; } = string.Empty;

    #endregion BuildNowPlayingEmbed

    #region BuildSearchSongRequestsEmbed

    public string EmbedAzuraSearchSongRequestsTitle { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsAvaDesc { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsAvaSong { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsAvaArtist { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsAvaAlbum { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsFoundDesc { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsFoundSong { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsFoundArtist { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsFoundAlbum { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsFoundAlbumNotAvailable { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsFoundFooter { get; set; } = string.Empty;

    #endregion BuildSearchSongRequestsEmbed

    #region BuildRequestNotAvailableEmbed

    public string EmbedAzuraSearchSongRequestsNotDesc { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsNotFooter { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsNotAvaTitle { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsNotAvaDesc { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsNotAvaSong { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsNotAvaArtist { get; set; } = string.Empty;

    #endregion BuildRequestNotAvailableEmbed

    #region BuildRequestSongEmbed

    public string EmbedAzuraSearchSongRequestsRequestTitle { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsRequestDesc { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsRequestSong { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsRequestArtist { get; set; } = string.Empty;
    public string EmbedAzuraSearchSongRequestsRequestAlbum { get; set; } = string.Empty;

    #endregion BuildRequestSongEmbed

    #region BuildFavouriteSongEmbed

    public string EmbedAzuraFavoriteSongTitle { get; set; } = string.Empty;
    public string EmbedAzuraFavoriteSongDescIsUser { get; set; } = string.Empty;
    public string EmbedAzuraFavoriteSongDescIsNot { get; set; } = string.Empty;
    public string EmbedAzuraFavoriteSongSong { get; set; } = string.Empty;
    public string EmbedAzuraFavoriteSongArtist { get; set; } = string.Empty;
    public string EmbedAzuraFavoriteSongAlbum { get; set; } = string.Empty;
    public string EmbedAzuraFavoriteSongAlbumNotAvailable { get; set; } = string.Empty;

    #endregion BuildFavouriteSongEmbed

    #endregion Embeds
}
