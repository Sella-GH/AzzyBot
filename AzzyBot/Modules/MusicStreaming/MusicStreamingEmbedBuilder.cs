using System;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.MusicStreaming.Player;
using AzzyBot.Strings.MusicStreaming;
using DSharpPlus.Entities;
using Lavalink4NET.Integrations.LyricsJava;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingEmbedBuilder
{
    internal static DiscordEmbed BuildLyricsEmbed(string userName, string userAvatarUrl, Lyrics lyrics, string artist, string song)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = MusicStreamingStringBuilder.GetEmbedsLyricsTitle;
        string message = MusicStreamingStringBuilder.GetEmbedsLyricsMessageNotFound;

        if (!string.IsNullOrWhiteSpace(lyrics.Text))
            message = lyrics.Text;

        if (message.Length > 4096)
            message = MusicStreamingStringBuilder.GetEmbedsLyricsMessageTooBig + $"\n{lyrics.Source}";

        string footerText = MusicStreamingStringBuilder.GetEmbedsLyricsFooter(song, artist);

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, string.Empty, footerText);
    }

    internal static DiscordEmbed BuildPreconditionErrorEmbed(string userName, string userAvatarUrl, in PlayerResult<MusicStreamingPlayer> result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = MusicStreamingStringBuilder.GetEmbedsPreconditionTitle;
        string message = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => MusicStreamingStringBuilder.GetEmbedsPreconditionNotInVoice,
            PlayerRetrieveStatus.BotNotConnected => MusicStreamingStringBuilder.GetEmbedsPreconditionBotNotInVoice,
            PlayerRetrieveStatus.VoiceChannelMismatch => MusicStreamingStringBuilder.GetEmbedsPreconditionVoiceMismatch,

            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Playing => MusicStreamingStringBuilder.GetEmbedsPreconditionVoiceNotPlaying,
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPlaying => MusicStreamingStringBuilder.GetEmbedsPreconditionVoiceAlreadyPlaying,
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPaused => MusicStreamingStringBuilder.GetEmbedsPreconditionVoiceNotPaused,
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Paused => MusicStreamingStringBuilder.GetEmbedsPreconditionVoiceAlreadyPaused,
            _ => MusicStreamingStringBuilder.GetEmbedsPreconditionError
        };

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl);
    }
}
