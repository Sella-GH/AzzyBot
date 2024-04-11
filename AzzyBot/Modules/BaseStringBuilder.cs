using System;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Modules;
using AzzyBot.Modules.AzuraCast.Strings;
using AzzyBot.Modules.ClubManagement.Strings;
using AzzyBot.Modules.Core.Strings;
using AzzyBot.Modules.MusicStreaming.Strings;

namespace AzzyBot.Strings;

internal abstract class BaseStringBuilder
{
    internal static async Task LoadStringsAsync()
    {
        if (!await CoreStringBuilder.LoadCoreStringsAsync())
            throw new InvalidOperationException("Core strings can't be loaded");

        if (ModuleStates.AzuraCast && !await AzuraCastStringBuilder.LoadAzuraCastStringsAsync())
            throw new InvalidOperationException("AzuraCast strings can't be loaded");

        if (ModuleStates.ClubManagement && !await CmStringBuilder.LoadClubManagementStringsAsync())
            throw new InvalidOperationException("ClubManagement strings can't be loaded");

        if (ModuleStates.MusicStreaming && !await MsStringBuilder.LoadMusicStreamingStringsAsync())
            throw new InvalidOperationException("MusicStreaming strings can't be loaded");
    }

    protected static string BuildString(string template, string parameterName, double parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, int parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, string parameterValue) => template.Replace(parameterName, parameterValue, StringComparison.InvariantCultureIgnoreCase);
}
