using System.Threading.Tasks;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Strings;
using Newtonsoft.Json;

namespace AzzyBot.Modules.MusicStreaming.Strings;

internal sealed class MusicStreamingStringBuilder : BaseStringBuilder
{
    private static MusicStreamingStringModel Model = new();

    internal static async Task<bool> LoadMusicStreamingStringsAsync()
    {
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.MusicStreaming)];
        string content = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.StringsMusicStreamingJSON), directories);

        if (!string.IsNullOrWhiteSpace(content))
        {
            MusicStreamingStringModel? newModel = JsonConvert.DeserializeObject<MusicStreamingStringModel>(content);
            if (newModel is not null)
            {
                // Reference assignment is atomic in .NET, so this is thread safe.
                Model = newModel;
            }
        }

        return Model is not null;
    }

    #region Commands

    #region CommandsDisconnect

    internal static string GetCommandsDisconnectVoiceRequired => Model.CommandsDisconnectVoiceRequired;
    internal static string GetCommandsDisconnectVoiceBotIsDisc => Model.CommandsDisconnectVoiceBotIsDisc;
    internal static string GetCommandsDisconnectVoiceSuccess => Model.CommandsDisconnectVoiceSuccess;

    #endregion CommandsDisconnect

    #region CommandsJoin

    internal static string GetCommandsJoinVoiceRequired => Model.CommandsJoinVoiceRequired;
    internal static string GetCommandsJoinVoiceBotIsThere => Model.CommandsJoinVoiceBotIsThere;
    internal static string GetCommandsJoinVoiceSuccess => Model.CommandsJoinVoiceSuccess;

    #endregion CommandsJoin

    #region CommandsSetVolume

    internal static string GetCommandsSetVolumeVoiceRequired => Model.CommandsSetVolumeVoiceRequired;
    internal static string GetCommandsSetVolumeVoiceSuccess(double volume) => BuildString(Model.CommandsSetVolumeVoiceSuccess, "%VALUE%", volume);
    internal static string GetCommandsSetVolumeVoiceInvalid => Model.CommandsSetVolumeVoiceInvalid;

    #endregion CommandsSetVolume

    #region CommandsShowLyrics

    internal static string GetCommandsShowLyricsModuleRequired => Model.CommandsShowLyricsModuleRequired;

    #endregion CommandsShowLyrics

    #region CommandsStart

    internal static string GetCommandsStartVoiceRequired => Model.CommandsStartVoiceRequired;
    internal static string GetCommandsStartVoiceMusicPlaying => Model.CommandsStartVoiceMusicPlaying;

    #endregion CommandsStart

    #region CommandsStop

    internal static string GetCommandsStopVoiceRequired => Model.CommandsStopVoiceRequired;
    internal static string GetCommandsStopVoiceMusicPlaying => Model.CommandsStopVoiceMusicPlaying;

    #endregion CommandsStop

    #region CustomPlayerMessages

    internal static string GetCustomPlayerIsActiveAgain => Model.CustomPlayerIsActiveAgain;
    internal static string GetCustomPlayerIsInactivePlaying(int minutes) => BuildString(Model.CustomPlayerIsInactivePlaying, "%MINUTES%", minutes);
    internal static string GetCustomPlayerIsInactiveUsers(int minutes) => BuildString(Model.CustomPlayerIsInactiveUsers, "%MINUTES%", minutes);
    internal static string GetCustomPlayerLeaves => Model.CustomPlayerLeaves;

    #endregion CustomPlayerMessages

    #endregion Commands

    #region Embeds

    #region EmbedsLyrics

    internal static string GetEmbedsLyricsTitle => Model.EmbedsLyricsTitle;
    internal static string GetEmbedsLyricsMessageNotFound => Model.EmbedsLyricsMessageNotFound;
    internal static string GetEmbedsLyricsMessageTooBig => Model.EmbedsLyricsMessageTooBig;
    internal static string GetEmbedsLyricsFooter(string song, string artist) => BuildString(BuildString(Model.EmbedsLyricsFooter, "%SONG%", song), "%ARTIST%", artist);

    #endregion EmbedsLyrics

    #region EmbedsPrecondition

    internal static string GetEmbedsPreconditionTitle => Model.EmbedsPreconditionTitle;
    internal static string GetEmbedsPreconditionNotInVoice => Model.EmbedsPreconditionNotInVoice;
    internal static string GetEmbedsPreconditionBotNotInVoice => Model.EmbedsPreconditionBotNotInVoice;
    internal static string GetEmbedsPreconditionVoiceMismatch => Model.EmbedsPreconditionVoiceMismatch;
    internal static string GetEmbedsPreconditionVoiceNotPlaying => Model.EmbedsPreconditionVoiceNotPlaying;
    internal static string GetEmbedsPreconditionVoiceAlreadyPlaying => Model.EmbedsPreconditionVoiceAlreadyPlaying;
    internal static string GetEmbedsPreconditionVoiceNotPaused => Model.EmbedsPreconditionVoiceNotPaused;
    internal static string GetEmbedsPreconditionVoiceAlreadyPaused => Model.EmbedsPreconditionVoiceAlreadyPaused;
    internal static string GetEmbedsPreconditionError => Model.EmbedsPreconditionError;

    #endregion EmbedsPrecondition

    #endregion Embeds
}
