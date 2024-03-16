using System;
using AzzyBot.Modules.Core;
using AzzyBot.Strings.MusicStreaming;
using DSharpPlus.Entities;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingEmbedBuilder
{
    internal static DiscordEmbed BuildLyricsEmbed(string userName, string userAvatarUrl, string text, string artist, string song)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = MusicStreamingStringBuilder.GetEmbedsLyricsTitle;
        string message = MusicStreamingStringBuilder.GetEmbedsLyricsMessageNotFound;

        if (!string.IsNullOrWhiteSpace(text))
            message = text;

        string footerText = MusicStreamingStringBuilder.GetEmbedsLyricsFooter(song, artist);

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl, DiscordColor.SpringGreen, string.Empty, footerText);
    }

    internal static DiscordEmbed BuildPreconditionErrorEmbed(string userName, string userAvatarUrl, in PlayerResult<LavalinkPlayer> result)
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
