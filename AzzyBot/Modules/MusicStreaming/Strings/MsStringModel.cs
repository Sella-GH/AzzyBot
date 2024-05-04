namespace AzzyBot.Modules.MusicStreaming.Strings;

internal sealed class MsStringModel
{
    #region Commands

    #region CommandsDisconnect

    public string CommandsDisconnectVoiceBotIsDisc { get; set; } = string.Empty;
    public string CommandsDisconnectVoiceSuccess { get; set; } = string.Empty;

    #endregion CommandsDisconnect

    #region CommandsJoin

    public string CommandsJoinVoiceBotIsThere { get; set; } = string.Empty;
    public string CommandsJoinVoiceSuccess { get; set; } = string.Empty;

    #endregion CommandsJoin

    #region CommandsSetVolume

    public string CommandsSetVolumeVoiceSuccess { get; set; } = string.Empty;
    public string CommandsSetVolumeVoiceInvalid { get; set; } = string.Empty;

    #endregion CommandsSetVolume

    #region CommandsShowLyrics

    public string CommandsShowLyricsModuleRequired { get; set; } = string.Empty;

    #endregion CommandsShowLyrics

    #region CommandsStart

    public string CommandsStartVoiceMusicPlaying { get; set; } = string.Empty;

    #endregion CommandsStart

    #region CommandsStop

    public string CommandsStopVoiceMusicPlaying { get; set; } = string.Empty;

    #endregion CommandsStop

    #region CustomPlayerMessages

    public string CustomPlayerIsActivePlayingAgain { get; set; } = string.Empty;
    public string CustomPlayerIsActiveUsersAgain { get; set; } = string.Empty;
    public string CustomPlayerIsInactivePlaying { get; set; } = string.Empty;
    public string CustomPlayerIsInactiveUsers { get; set; } = string.Empty;
    public string CustomPlayerLeaves { get; set; } = string.Empty;

    #endregion CustomPlayerMessages

    #endregion Commands

    #region Embeds

    #region EmbedsLyrics

    public string EmbedsLyricsTitle { get; set; } = string.Empty;
    public string EmbedsLyricsMessageNotFound { get; set; } = string.Empty;
    public string EmbedsLyricsMessageTooBig { get; set; } = string.Empty;
    public string EmbedsLyricsFooter { get; set; } = string.Empty;

    #endregion EmbedsLyrics

    #region EmbedsPrecondition

    public string EmbedsPreconditionTitle { get; set; } = string.Empty;
    public string EmbedsPreconditionNotInVoice { get; set; } = string.Empty;
    public string EmbedsPreconditionBotNotInVoice { get; set; } = string.Empty;
    public string EmbedsPreconditionVoiceMismatch { get; set; } = string.Empty;
    public string EmbedsPreconditionVoiceNotPlaying { get; set; } = string.Empty;
    public string EmbedsPreconditionVoiceAlreadyPlaying { get; set; } = string.Empty;
    public string EmbedsPreconditionVoiceNotPaused { get; set; } = string.Empty;
    public string EmbedsPreconditionVoiceAlreadyPaused { get; set; } = string.Empty;
    public string EmbedsPreconditionError { get; set; } = string.Empty;

    #endregion EmbedsPrecondition

    #endregion Embeds
}
