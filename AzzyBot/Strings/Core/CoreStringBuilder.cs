using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Structs;
using Newtonsoft.Json;

namespace AzzyBot.Strings.Core;

internal sealed class CoreStringBuilder : StringBuilding
{
    private static CoreStringModel Model = new();

    internal static async Task<bool> LoadCoreStringsAsync()
    {
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

        return Model is not null;
    }

    #region CommandsBotRestart

    internal static string GetCommandsBotRestart => Model.CommandsBotRestart;

    #endregion CommandsBotRestart

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

    [SuppressMessage("Roslynator", "RCS1250:Use implicit/explicit object creation", Justification = "Collection Expression are not yet available for Dictionaries.")]
    internal static Dictionary<string, DiscordEmbedStruct> GetEmbedAzzyStatsFields(long uptime, int ping, string coreUsage, double oneMinLoad, double fiveMinLoad, double fifteenMinLoad, double memUsage, double azzyMem, double memTotal, string diskUsage)
    {
        return new Dictionary<string, DiscordEmbedStruct>()
        {
            [Model.EmbedAzzyStatsSystemUptimeTitle] = new(Model.EmbedAzzyStatsSystemUptimeTitle, BuildString(Model.EmbedAzzyStatsSystemUptimeDesc, "%VALUE%", $"<t:{uptime}>"), false),
            [Model.EmbedAzzyStatsPingTitle] = new(Model.EmbedAzzyStatsPingTitle, BuildString(Model.EmbedAzzyStatsPingDesc, "%VALUE%", ping), false),
            [Model.EmbedAzzyStatsCpuUsageTitle] = new(Model.EmbedAzzyStatsCpuUsageTitle, coreUsage, false),
            [Model.EmbedAzzyStats1MinLoadTitle] = new(Model.EmbedAzzyStats1MinLoadTitle, BuildString(Model.EmbedAzzyStats1MinLoadDesc, "%VALUE%", oneMinLoad), true),
            [Model.EmbedAzzyStats5MinLoadTitle] = new(Model.EmbedAzzyStats5MinLoadTitle, BuildString(Model.EmbedAzzyStats5MinLoadDesc, "%VALUE%", fiveMinLoad), true),
            [Model.EmbedAzzyStats15MinLoadTitle] = new(Model.EmbedAzzyStats15MinLoadTitle, BuildString(Model.EmbedAzzyStats15MinLoadDesc, "%VALUE%", fifteenMinLoad), true),
            [Model.EmbedAzzyStatsRamUsageTitle] = new(Model.EmbedAzzyStatsRamUsageTitle, BuildString(BuildString(BuildString(Model.EmbedAzzyStatsRamUsageDesc, "%USED%", memUsage), "%BOT%", azzyMem), "%TOTAL%", memTotal), false),
            [Model.EmbedAzzyStatsDiskUsageTitle] = new(Model.EmbedAzzyStatsDiskUsageTitle, diskUsage, false)
        };
    }

    internal static string GetEmbedAzzyStatsNetworkUsageTitle(string name) => BuildString(Model.EmbedAzzyStatsNetworkUsageTitle, "%NAME%", name);
    internal static string GetEmbedAzzyStatsNetworkUsageDesc(double receive, double transmit) => BuildString(BuildString(Model.EmbedAzzyStatsNetworkUsageDesc, "%RECEIVE%", receive), "%TRANSMIT%", transmit);

    #endregion BuildAzzyStatsEmbed

    #region BuildAzzyStatsNotAvailableEmbed

    internal static string GetEmbedAzzyStatsNotAvailableTitle => Model.EmbedAzzyStatsNotAvailableTitle;
    internal static string GetEmbedAzzyStatsNotAvailableDesc => Model.EmbedAzzyStatsNotAvailableDesc;

    #endregion BuildAzzyStatsNotAvailableEmbed

    #region BuildInfoAzzyEmbed

    internal static string GetEmbedAzzyInfoTitle => Model.EmbedAzzyInfoTitle;
    internal static string EmbedAzzyInfoBotName => Model.EmbedAzzyInfoBotName;
    internal static string EmbedAzzyInfoBotUptime => Model.EmbedAzzyInfoBotUptime;
    internal static string EmbedAzzyInfoBotVersion => Model.EmbedAzzyInfoBotVersion;
    internal static string EmbedAzzyInfoNetVersion => Model.EmbedAzzyInfoNetVersion;
    internal static string EmbedAzzyInfoDspVersion => Model.EmbedAzzyInfoDspVersion;
    internal static string EmbedAzzyInfoGitHubCommit => Model.EmbedAzzyInfoGitHubCommit;
    internal static string EmbedAzzyInfoCompDate => Model.EmbedAzzyInfoCompDate;
    internal static string EmbedAzzyInfoEnv => Model.EmbedAzzyInfoEnv;
    internal static string EmbedAzzyInfoModules => Model.EmbedAzzyInfoModules;

    #endregion BuildInfoAzzyEmbed

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

    #region Updater

    internal static string GetUpdatesAvailable(string product, string version) => BuildString(BuildString(Model.UpdatesAvailable, "%PRODUCT%", product), "%VERSION%", version);

    #endregion Updater
}
