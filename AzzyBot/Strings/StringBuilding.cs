using System;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Modules;
using AzzyBot.Strings.AzuraCast;
using AzzyBot.Strings.ClubManagement;
using AzzyBot.Strings.Core;
using AzzyBot.Strings.MusicStreaming;

namespace AzzyBot.Strings;

internal abstract class StringBuilding
{
    internal static async Task LoadStringsAsync()
    {
        if (!await CoreStringBuilder.LoadCoreStringsAsync())
            throw new InvalidOperationException("Core strings can't be loaded");

        if (BaseSettings.ActivateAzuraCast && !await AzuraCastStringBuilder.LoadAzuraCastStringsAsync())
            throw new InvalidOperationException("AzuraCast strings can't be loaded");

        if (BaseSettings.ActivateClubManagement && !await ClubManagementStringBuilder.LoadClubManagementStringsAsync())
            throw new InvalidOperationException("ClubManagement strings can't be loaded");

        if (BaseSettings.ActivateMusicStreaming && !await MusicStreamingStringBuilder.LoadMusicStreamingStringsAsync())
            throw new InvalidOperationException("MusicStreaming strings can't be loaded");
    }

    protected static string BuildString(string template, string parameterName, double parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, int parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, string parameterValue) => template.Replace(parameterName, parameterValue, StringComparison.InvariantCultureIgnoreCase);
}
