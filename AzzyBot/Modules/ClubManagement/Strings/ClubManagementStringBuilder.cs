using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Structs;
using AzzyBot.Strings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzzyBot.Modules.ClubManagement.Strings;

internal sealed class ClubManagementStringBuilder : BaseStringBuilder
{
    private static ClubManagementStringModel Model = new();

    internal static async Task<bool> LoadClubManagementStringsAsync()
    {
        ExceptionHandler.LogMessage(LogLevel.Debug, "Loading ClubManagement Strings");

        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.ClubManagement)];
        string content = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.StringsClubManagementJSON), directories);

        if (!string.IsNullOrWhiteSpace(content))
        {
            ClubManagementStringModel? newModel = JsonConvert.DeserializeObject<ClubManagementStringModel>(content);
            if (newModel is not null)
            {
                // Reference assignment is atomic in .NET, so this is thread safe.
                Model = newModel;
            }
        }

        return Model is not null;
    }

    #region Commands

    #region CommandCloseClub

    internal static string CommandCloseClubAlreadyInitiated => Model.CommandCloseClubAlreadyInitiated;
    internal static string CommandCloseClubClubIsAlreadyClosed => Model.CommandCloseClubClubIsAlreadyClosed;
    internal static string CommandCloseClubClubClosed => Model.CommandCloseClubClubClosed;
    internal static string CommandCloseClubThreadTitle => Model.CommandCloseClubThreadTitle;
    internal static string CommandCloseClubThreadReason => Model.CommandCloseClubThreadReason;

    #endregion CommandCloseClub

    #region CommandOpenClub

    internal static string CommandOpenClubAlreadyOpen => Model.CommandOpenClubAlreadyOpen;
    internal static string CommandOpenClubClubOpened(string playlist) => BuildString(Model.CommandOpenClubClubOpened, "%PLAYLIST%", playlist);

    #endregion CommandOpenClub

    #endregion Commands

    #region Embeds

    #region BuildCloseClubEmbed

    internal static string EmbedCloseClubTitle => Model.EmbedCloseClubTitle;
    internal static string EmbedCloseClubDesc(string user) => (string.IsNullOrWhiteSpace(user)) ? Model.EmbedCloseClubDescAzzy : BuildString(Model.EmbedCloseClubDescUser, "%USER%", user);
    internal static string EmbedCloseClubDescInactivity => Model.EmbedCloseClubDescInactivity;
    internal static string EmbedCloseClubFooter => Model.EmbedCloseClubFooter;

    #endregion BuildCloseClubEmbed

    #region BuildOpenClubEmbed

    internal static string EmbedOpenClubTitle => Model.EmbedOpenClubTitle;
    internal static string EmbedOpenClubDesc(string user) => BuildString(Model.EmbedOpenClubDesc, "%USER%", user);
    internal static string EmbedOpenClubFooter => Model.EmbedOpenClubFooter;

    #endregion BuildOpenClubEmbed

    #region BuildClubStatisticsEmbed

    internal static string EmbedClubStatisticsTitle => Model.EmbedClubStatisticsTitle;
    internal static string EmbedClubStatisticsOpeningTime => Model.EmbedClubStatisticsOpeningTime;
    internal static string EmbedClubStatisticsClosingTime => Model.EmbedClubStatisticsClosingTime;
    internal static string EmbedClubStatisticsOpeningDuration => Model.EmbedClubStatisticsOpeningDuration;
    internal static string EmbedClubStatisticsVisitors => Model.EmbedClubStatisticsVisitors;
    internal static string EmbedClubStatisticsPeakListeners => Model.EmbedClubStatisticsPeakListeners;
    internal static string EmbedClubStatisticsSongsPlayed => Model.EmbedClubStatisticsSongsPlayed;
    internal static string EmbedClubStatisticsSongRequestsNumber => Model.EmbedClubStatisticsSongRequestsNumber;
    internal static string EmbedClubStatisticsPlayedPlaylists => Model.EmbedClubStatisticsPlayedPlaylists;
    internal static string EmbedClubStatisticsOutgoingTraffic => Model.EmbedClubStatisticsOutgoingTraffic;

    internal static Dictionary<string, DiscordEmbedStruct> EmbedClubStatisticsFields(long openingTime, long closingTime, in TimeSpan openingDuration, int peakListeners, int visitors, int songs, int songRequests, string playlists)
    {
        return new()
        {
            [EmbedClubStatisticsOpeningTime] = new(nameof(EmbedClubStatisticsOpeningTime), $"<t:{openingTime}>", false),
            [EmbedClubStatisticsClosingTime] = new(nameof(EmbedClubStatisticsClosingTime), $"<t:{closingTime}>", false),
            [EmbedClubStatisticsOpeningDuration] = new(nameof(EmbedClubStatisticsOpeningDuration), $"{openingDuration.Hours:D2}:{openingDuration.Minutes:D2}:{openingDuration.Seconds:D2}", false),
            [EmbedClubStatisticsPeakListeners] = new(nameof(EmbedClubStatisticsPeakListeners), peakListeners.ToString(CultureInfo.InvariantCulture), false),
            [EmbedClubStatisticsVisitors] = new(nameof(EmbedClubStatisticsVisitors), visitors.ToString(CultureInfo.InvariantCulture), false),
            [EmbedClubStatisticsSongsPlayed] = new(nameof(EmbedClubStatisticsSongsPlayed), songs.ToString(CultureInfo.InvariantCulture), false),
            [EmbedClubStatisticsSongRequestsNumber] = new(nameof(EmbedClubStatisticsSongRequestsNumber), songRequests.ToString(CultureInfo.InvariantCulture), false),
            [EmbedClubStatisticsPlayedPlaylists] = new(nameof(EmbedClubStatisticsPlayedPlaylists), playlists, false)
        };
    }

    #endregion BuildClubStatisticsEmbed

    #endregion Embeds
}
