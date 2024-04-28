using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Structs;
using AzzyBot.Strings;
using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Strings;

internal sealed class CoreStringBuilder : BaseStringBuilder
{
    private static CoreStringModel Model = new();

    internal static async Task<bool> LoadCoreStringsAsync()
    {
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Loading Core Strings", null);

        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.Core)];
        string content = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.StringsCoreJSON), directories);

        if (!string.IsNullOrWhiteSpace(content))
        {
            CoreStringModel? newModel = JsonConvert.DeserializeObject<CoreStringModel>(content);
            if (newModel is not null)
            {
                // Reference assignment is atomic in .NET, so this is thread safe.
                Model = newModel;
            }
        }

        return Model is not null && CheckStrings(Model);
    }

    #region Embeds

    #region BuildAzzyHelpEmbed

    internal static string GetEmbedAzzyHelpTitle => Model.EmbedAzzyHelpTitle;
    internal static string GetEmbedAzzyHelpDesc => Model.EmbedAzzyHelpDesc;
    internal static string GetEmbedAzzyHelpOptionDesc => Model.EmbedAzzyHelpOptionDesc;

    #endregion BuildAzzyHelpEmbed

    #region BuildAzzyStatsEmbed

    internal static string GetEmbedAzzyStatsTitle => Model.EmbedAzzyStatsTitle;
    internal static string GetEmbedAzzyStatsCpuUsageAll(double value) => BuildString(Model.EmbedAzzyStatsCpuUsageAll, "%VALUE%", value);
    internal static string GetEmbedAzzyStatsCpuUsageCore(int counter, double value) => BuildString(BuildString(Model.EmbedAzzyStatsCpuUsageCore, "%COUNTER%", counter), "%VALUE%", value);
    internal static string GetEmbedAzzyStatsDiskUsageDesc(double used, double total) => BuildString(BuildString(Model.EmbedAzzyStatsDiskUsageDesc, "%USED%", used), "%TOTAL%", total);

    internal static Dictionary<string, DiscordEmbedStruct> GetEmbedAzzyStatsFields(string os, string osArch, long uptime, int ping, string coreUsage, double oneMinLoad, double fiveMinLoad, double fifteenMinLoad, double memUsage, double azzyMem, double memTotal, string diskUsage)
    {
        Dictionary<string, DiscordEmbedStruct> fields = [];

        if (!string.IsNullOrWhiteSpace(os))
            fields.Add(Model.EmbedAzzyStatsOperatingSystem, new(Model.EmbedAzzyStatsOperatingSystem, os, true));

        if (!string.IsNullOrWhiteSpace(osArch))
            fields.Add(Model.EmbedAzzyStatsOperatingSystemArchitecture, new(Model.EmbedAzzyStatsOperatingSystemArchitecture, osArch, true));

        if (uptime is not 0)
            fields.Add(Model.EmbedAzzyStatsSystemUptimeTitle, new(Model.EmbedAzzyStatsSystemUptimeTitle, BuildString(Model.EmbedAzzyStatsSystemUptimeDesc, "%VALUE%", $"<t:{uptime}>"), false));

        fields.Add(Model.EmbedAzzyStatsPingTitle, new(Model.EmbedAzzyStatsPingTitle, BuildString(Model.EmbedAzzyStatsPingDesc, "%VALUE%", ping), false));

        if (!string.IsNullOrWhiteSpace(coreUsage))
            fields.Add(Model.EmbedAzzyStatsCpuUsageTitle, new(Model.EmbedAzzyStatsCpuUsageTitle, coreUsage, false));

        fields.Add(Model.EmbedAzzyStats1MinLoadTitle, new(Model.EmbedAzzyStats1MinLoadTitle, BuildString(Model.EmbedAzzyStats1MinLoadDesc, "%VALUE%", oneMinLoad), true));
        fields.Add(Model.EmbedAzzyStats5MinLoadTitle, new(Model.EmbedAzzyStats5MinLoadTitle, BuildString(Model.EmbedAzzyStats5MinLoadDesc, "%VALUE%", fiveMinLoad), true));
        fields.Add(Model.EmbedAzzyStats15MinLoadTitle, new(Model.EmbedAzzyStats15MinLoadTitle, BuildString(Model.EmbedAzzyStats15MinLoadDesc, "%VALUE%", fifteenMinLoad), true));

        if (memUsage is not 0 && memTotal is not 0)
            fields.Add(Model.EmbedAzzyStatsRamUsageTitle, new(Model.EmbedAzzyStatsRamUsageTitle, BuildString(BuildString(Model.EmbedAzzyStatsRamUsageDesc, "%USED%", memUsage), "%TOTAL%", memTotal), false));

        if (azzyMem is not 0)
            fields.Add(Model.EmbedAzzyStatsRamUsageAzzyTitle, new(Model.EmbedAzzyStatsRamUsageAzzyTitle, BuildString(Model.EmbedAzzyStatsRamUsageAzzyDesc, "%BOT%", azzyMem), false));

        if (!string.IsNullOrWhiteSpace(diskUsage))
            fields.Add(Model.EmbedAzzyStatsDiskUsageTitle, new(Model.EmbedAzzyStatsDiskUsageTitle, diskUsage, false));

        return fields;
    }

    internal static string GetEmbedAzzyStatsNetworkUsageTitle(string name) => BuildString(Model.EmbedAzzyStatsNetworkUsageTitle, "%NAME%", name);
    internal static string GetEmbedAzzyStatsNetworkUsageDesc(double receive, double transmit) => BuildString(BuildString(Model.EmbedAzzyStatsNetworkUsageDesc, "%RECEIVE%", receive), "%TRANSMIT%", transmit);
    internal static string GetEmbedAzzyStatsMoreStats => Model.EmbedAzzyStatsMoreStats;

    #endregion BuildAzzyStatsEmbed

    #region BuildInfoAzzyEmbed

    internal static string GetEmbedAzzyInfoTitle => Model.EmbedAzzyInfoTitle;
    internal static string EmbedAzzyInfoBotName => Model.EmbedAzzyInfoBotName;
    internal static string EmbedAzzyInfoBotUptime => Model.EmbedAzzyInfoBotUptime;
    internal static string EmbedAzzyInfoBotVersion => Model.EmbedAzzyInfoBotVersion;
    internal static string EmbedAzzyInfoNetVersion => Model.EmbedAzzyInfoNetVersion;
    internal static string EmbedAzzyInfoDspVersion => Model.EmbedAzzyInfoDspVersion;
    internal static string EmbedAzzyInfoAuthors => Model.EmbedAzzyInfoAuthors;
    internal static string EmbedAzzyInfoRepository => Model.EmbedAzzyInfoRepository;
    internal static string EmbedAzzyInfoLanguage => Model.EmbedAzzyInfoLanguage;
    internal static string EmbedAzzyInfoSourceCode => Model.EmbedAzzyInfoSourceCode;
    internal static string EmbedAzzyInfoCustomizationCode => Model.EmbedAzzyInfoCustomizationCode;
    internal static string EmbedAzzyInfoGitHubCommit => Model.EmbedAzzyInfoGitHubCommit;
    internal static string EmbedAzzyInfoCompDate => Model.EmbedAzzyInfoCompDate;
    internal static string EmbedAzzyInfoEnv => Model.EmbedAzzyInfoEnv;
    internal static string EmbedAzzyInfoModules => Model.EmbedAzzyInfoModules;

    #endregion BuildInfoAzzyEmbed

    #region BuildUpdatesAvailableEmbed

    internal static string GetEmbedUpdatesAvailableTitle => Model.EmbedUpdatesAvailableTitle;
    internal static string GetEmbedUpdatesAvailableDesc => Model.EmbedUpdatesAvailableDesc;
    internal static string GetEmbedUpdatesAvailableReleaseDate => Model.EmbedUpdatesAvailableReleaseDate;
    internal static string GetEmbedUpdatesAvailableYourVersion => Model.EmbedUpdatesAvailableYourVersion;
    internal static string GetEmbedUpdatesAvailableUpdatedVersion => Model.EmbedUpdatesAvailableUpdatedVersion;

    #endregion BuildUpdatesAvailableEmbed

    #region BuildUpdatesAvailableChangelogEmbed

    internal static string GetEmbedUpdatesAvailableChangelogTitle => Model.EmbedUpdatesAvailableChangelogTitle;
    internal static string GetEmbedUpdatesAvailableChangelogTooBig(string url) => BuildString(Model.EmbedUpdatesAvailableChangelogTooBig, "%URL%", url);

    #endregion BuildUpdatesAvailableChangelogEmbed

    #endregion Embeds

    #region ExceptionHandling

    #region DiscordClientError

    internal static string GetExceptionHandlingDiscordPermissions(string user) => BuildString(Model.ExceptionHandlingDiscordPermissions, "%USER%", user);

    #endregion DiscordClientError

    #region ExceptionHandler

    internal static string GetExceptionHandlingErrorDiscovered(string user) => BuildString(Model.ExceptionHandlingErrorDiscovered, "%USER%", user);

    #endregion ExceptionHandler

    #region SlashCommandError

    internal static string GetExceptionHandlingNotInGuild => Model.ExceptionHandlingNotInGuild;
    internal static string GetExceptionHandlingOnCooldown => Model.ExceptionHandlingOnCooldown;
    internal static string GetExceptionHandlingDefault => Model.ExceptionHandlingDefault;

    #endregion SlashCommandError

    #endregion ExceptionHandling
}
