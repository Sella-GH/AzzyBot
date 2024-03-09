namespace AzzyBot.Strings.Core;

internal sealed class CoreStringModel
{
    #region CommandsBotRestart

    public string CommandsBotRestart { get; set; } = string.Empty;

    #endregion CommandsBotRestart

    #region Embeds

    #region BuildAzzyHelpEmbed

    public string EmbedAzzyHelpTitle { get; set; } = string.Empty;
    public string EmbedAzzyHelpDesc { get; set; } = string.Empty;
    public string EmbedAzzyHelpOptionDesc { get; set; } = string.Empty;

    #endregion BuildAzzyHelpEmbed

    #region BuildAzzyStatsEmbed

    public string EmbedAzzyStatsTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsSystemUptimeTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsSystemUptimeDesc { get; set; } = string.Empty;
    public string EmbedAzzyStatsPingTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsPingDesc { get; set; } = string.Empty;
    public string EmbedAzzyStatsCpuUsageTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsCpuUsageAll { get; set; } = string.Empty;
    public string EmbedAzzyStatsCpuUsageCore { get; set; } = string.Empty;
    public string EmbedAzzyStats1MinLoadTitle { get; set; } = string.Empty;
    public string EmbedAzzyStats1MinLoadDesc { get; set; } = string.Empty;
    public string EmbedAzzyStats5MinLoadTitle { get; set; } = string.Empty;
    public string EmbedAzzyStats5MinLoadDesc { get; set; } = string.Empty;
    public string EmbedAzzyStats15MinLoadTitle { get; set; } = string.Empty;
    public string EmbedAzzyStats15MinLoadDesc { get; set; } = string.Empty;
    public string EmbedAzzyStatsRamUsageTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsRamUsageDesc { get; set; } = string.Empty;
    public string EmbedAzzyStatsDiskUsageTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsDiskUsageDesc { get; set; } = string.Empty;
    public string EmbedAzzyStatsNetworkUsageTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsNetworkUsageDesc { get; set; } = string.Empty;

    #endregion BuildAzzyStatsEmbed

    #region BuildAzzyStatsNotAvailableEmbed

    public string EmbedAzzyStatsNotAvailableTitle { get; set; } = string.Empty;
    public string EmbedAzzyStatsNotAvailableDesc { get; set; } = string.Empty;

    #endregion BuildAzzyStatsNotAvailableEmbed

    #region BuildInfoAzzyEmbed

    public string EmbedAzzyInfoTitle { get; set; } = string.Empty;
    public string EmbedAzzyInfoBotName { get; set; } = string.Empty;
    public string EmbedAzzyInfoBotUptime { get; set; } = string.Empty;
    public string EmbedAzzyInfoBotVersion { get; set; } = string.Empty;
    public string EmbedAzzyInfoNetVersion { get; set; } = string.Empty;
    public string EmbedAzzyInfoDspVersion { get; set; } = string.Empty;
    public string EmbedAzzyInfoGitHubCommit { get; set; } = string.Empty;
    public string EmbedAzzyInfoCompDate { get; set; } = string.Empty;
    public string EmbedAzzyInfoEnv { get; set; } = string.Empty;
    public string EmbedAzzyInfoModules { get; set; } = string.Empty;

    #endregion BuildInfoAzzyEmbed

    #endregion Embeds

    #region ExceptionHandling

    #region DiscordClientError

    public string ExceptionHandlingDiscordPermissions { get; set; } = string.Empty;

    #endregion DiscordClientError

    #region ExceptionHandler

    public string ExceptionHandlingErrorDiscovered { get; set; } = string.Empty;

    #endregion ExceptionHandler

    #region SlashCommandError

    public string ExceptionHandlingNotInGuild { get; set; } = string.Empty;
    public string ExceptionHandlingOnCooldown { get; set; } = string.Empty;
    public string ExceptionHandlingDefault { get; set; } = string.Empty;

    #endregion SlashCommandError

    #endregion ExceptionHandling

    #region Updater

    public string UpdatesAvailable { get; set; } = string.Empty;

    #endregion Updater
}
