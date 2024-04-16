using System;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.MusicStreaming.Player;
using AzzyBot.Modules.MusicStreaming.Strings;
using DSharpPlus.Entities;
using Lavalink4NET.Integrations.LyricsJava;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MsEmbedBuilder
{
    internal static DiscordEmbed BuildLyricsEmbed(string userName, string userAvatarUrl, Lyrics lyrics, string artist, string song)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = MsStringBuilder.GetEmbedsLyricsTitle;
        string message = MsStringBuilder.GetEmbedsLyricsMessageNotFound;

        if (!string.IsNullOrWhiteSpace(lyrics.Text))
            message = lyrics.Text;

        if (message.Length > 4096)
            message = MsStringBuilder.GetEmbedsLyricsMessageTooBig + $"\n{lyrics.Source}";

        string footerText = MsStringBuilder.GetEmbedsLyricsFooter(song, artist);

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, string.Empty, footerText);
    }

    internal static DiscordEmbed BuildPreconditionErrorEmbed(string userName, string userAvatarUrl, in PlayerResult<MsPlayer> result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = MsStringBuilder.GetEmbedsPreconditionTitle;
        string message = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => MsStringBuilder.GetEmbedsPreconditionNotInVoice,
            PlayerRetrieveStatus.BotNotConnected => MsStringBuilder.GetEmbedsPreconditionBotNotInVoice,
            PlayerRetrieveStatus.VoiceChannelMismatch => MsStringBuilder.GetEmbedsPreconditionVoiceMismatch,

            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Playing => MsStringBuilder.GetEmbedsPreconditionVoiceNotPlaying,
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPlaying => MsStringBuilder.GetEmbedsPreconditionVoiceAlreadyPlaying,
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPaused => MsStringBuilder.GetEmbedsPreconditionVoiceNotPaused,
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Paused => MsStringBuilder.GetEmbedsPreconditionVoiceAlreadyPaused,
            _ => MsStringBuilder.GetEmbedsPreconditionError
        };

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl);
    }
}
