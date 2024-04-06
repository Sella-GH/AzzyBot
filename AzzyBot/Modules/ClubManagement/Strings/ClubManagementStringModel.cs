namespace AzzyBot.Modules.ClubManagement.Strings;

internal sealed class ClubManagementStringModel
{
    #region Commands

    #region CommandCloseClub

    public string CommandCloseClubAlreadyInitiated { get; set; } = string.Empty;
    public string CommandCloseClubClubIsAlreadyClosed { get; set; } = string.Empty;
    public string CommandCloseClubClubClosed { get; set; } = string.Empty;
    public string CommandCloseClubThreadTitle { get; set; } = string.Empty;
    public string CommandCloseClubThreadReason { get; set; } = string.Empty;

    #endregion CommandCloseClub

    #region CommandOpenClub

    public string CommandOpenClubAlreadyOpen { get; set; } = string.Empty;
    public string CommandOpenClubClubOpened { get; set; } = string.Empty;

    #endregion CommandOpenClub

    #endregion Commands

    #region Embeds

    #region BuildCloseClubEmbed

    public string EmbedCloseClubTitle { get; set; } = string.Empty;
    public string EmbedCloseClubDescAzzy { get; set; } = string.Empty;
    public string EmbedCloseClubDescUser { get; set; } = string.Empty;
    public string EmbedCloseClubDescInactivity { get; set; } = string.Empty;
    public string EmbedCloseClubFooter { get; set; } = string.Empty;

    #endregion BuildCloseClubEmbed

    #region BuildOpenClubEmbed

    public string EmbedOpenClubTitle { get; set; } = string.Empty;
    public string EmbedOpenClubDesc { get; set; } = string.Empty;
    public string EmbedOpenClubFooter { get; set; } = string.Empty;

    #endregion BuildOpenClubEmbed

    #region BuildClubStatisticsEmbed

    public string EmbedClubStatisticsTitle { get; set; } = string.Empty;
    public string EmbedClubStatisticsOpeningTime { get; set; } = string.Empty;
    public string EmbedClubStatisticsClosingTime { get; set; } = string.Empty;
    public string EmbedClubStatisticsOpeningDuration { get; set; } = string.Empty;
    public string EmbedClubStatisticsVisitors { get; set; } = string.Empty;
    public string EmbedClubStatisticsPeakListeners { get; set; } = string.Empty;
    public string EmbedClubStatisticsSongsPlayed { get; set; } = string.Empty;
    public string EmbedClubStatisticsSongRequestsNumber { get; set; } = string.Empty;
    public string EmbedClubStatisticsPlayedPlaylists { get; set; } = string.Empty;
    public string EmbedClubStatisticsOutgoingTraffic { get; set; } = string.Empty;

    #endregion BuildClubStatisticsEmbed

    #endregion Embeds
}
