using System;
using AzzyBot.Modules.Core;
using DSharpPlus.Entities;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingEmbedBuilder
{
    internal static DiscordEmbed BuildPreconditionErrorEmbed(string userName, string userAvatarUrl, in PlayerResult<LavalinkPlayer> result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = "Error";
        string message = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel",
            PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected",
            PlayerRetrieveStatus.VoiceChannelMismatch => "You must be in the same voice channel as the bot.",

            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Playing => "The player is currently now playing any track.",
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPlaying => "The player is currently not playing any track.",
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPaused => "The player is already paused.",
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Paused => "The player is not paused.",
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.QueueEmpty => "The queue is empty.",
            _ => "An unknown error occurred"
        };

        return CoreEmbedBuilder.CreateBasicEmbed(title, message, userName, userAvatarUrl);
    }
}
