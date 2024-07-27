using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Rest.Entities;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services.Modules;

public sealed class MusicStreamingService(IAudioService audioService)
{
    private readonly IAudioService _audioService = audioService;

    public async Task<LavalinkPlayer?> GetLavalinkPlayerAsync(CommandContext context, bool connectToVoice = false, ImmutableArray<IPlayerPrecondition> preconditions = default)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        LavalinkPlayerOptions playerOptions = new() { SelfDeaf = true };
        PlayerRetrieveOptions retrieveOptions = new((connectToVoice) ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None, MemberVoiceStateBehavior.RequireSame, preconditions);
        ulong channelId = context.Member?.VoiceState.Channel?.Id ?? 0;

        PlayerResult<LavalinkPlayer> player = await _audioService.Players.RetrieveAsync(context.Guild.Id, channelId, PlayerFactory.Default, Options.Create(playerOptions), retrieveOptions);
        if (player.IsSuccess)
            return player.Player;

        string errorMessage = player.Status switch
        {
            PlayerRetrieveStatus.BotNotConnected => "I'm not connected to a voice channel.",
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You need to be in a voice channel to use this command.",
            PlayerRetrieveStatus.VoiceChannelMismatch => "You need to be in the same voice channel as me to use this command.",

            PlayerRetrieveStatus.PreconditionFailed when player.Precondition?.Equals(PlayerPrecondition.NotPaused) == true => "I'm not paused.",
            PlayerRetrieveStatus.PreconditionFailed when player.Precondition?.Equals(PlayerPrecondition.NotPlaying) == true => "I'm not playing music.",
            PlayerRetrieveStatus.PreconditionFailed when player.Precondition?.Equals(PlayerPrecondition.Paused) == true => "I'm already paused.",
            PlayerRetrieveStatus.PreconditionFailed when player.Precondition?.Equals(PlayerPrecondition.Playing) == true => "I'm already playing music.",

            _ => "An unknown error occurred while trying to retrieve the player."
        };

        await context.EditResponseAsync(errorMessage);

        return null;
    }

    public async Task<bool> CheckIfPlayedMusicIsStationAsync(CommandContext context, string station)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(station, nameof(station));

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, false);

        // Guild has no player
        if (player is null)
        {
            Console.Error.WriteLine("Guild has no player.");
            return false;
        }

        // Player doesn't plays anything
        Uri? playedUri = player.CurrentTrack?.Uri;
        if (playedUri is null)
        {
            Console.Error.WriteLine("Player doesn't plays anything.");
            return false;
        }

        Console.WriteLine($"Played URI: {playedUri}");

        bool playingHls = playedUri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"Playing HLS: {playingHls}");

        Uri stationUri = new((playingHls) ? station.Replace("listen", "live", StringComparison.OrdinalIgnoreCase) : station);
        Console.WriteLine($"Station URI: {stationUri}");

        int compareResult = Uri.Compare(playedUri, stationUri, UriComponents.Host, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"Compare result: {compareResult}");

        Console.WriteLine((compareResult is 0) && playedUri.AbsolutePath.StartsWith(stationUri.AbsolutePath, StringComparison.OrdinalIgnoreCase));

        return (Uri.Compare(playedUri, stationUri, UriComponents.Host, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) is 0) && playedUri.AbsolutePath.StartsWith(stationUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> JoinChannelAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, true);

        return player is not null;
    }

    public async Task<bool> PlayMusicAsync(CommandContext context, string mountPoint)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(mountPoint, nameof(mountPoint));

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, true);
        if (player is null)
            return false;

        TrackLoadOptions trackOptions = new()
        {
            CacheMode = CacheMode.Bypass,
            SearchBehavior = StrictSearchBehavior.Passthrough,
            SearchMode = TrackSearchMode.None
        };

        LavalinkTrack? track = await _audioService.Tracks.LoadTrackAsync(mountPoint, trackOptions);
        if (track is null)
        {
            await context.EditResponseAsync("An error occurred while trying to load the track.");
            return false;
        }

        await player.PlayAsync(track);

        return true;
    }

    public async Task<bool> SetVolumeAsync(CommandContext context, float volume, bool reset = false)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context);
        if (player is null)
            return false;

        if (reset)
        {
            await player.SetVolumeAsync(100 / 100f);
            return true;
        }

        await player.SetVolumeAsync(volume / 100f);

        return true;
    }

    public async Task<bool> StopMusicAsync(CommandContext context, bool disconnect = false)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, false, [PlayerPrecondition.Playing, PlayerPrecondition.NotPaused]);
        if (player is null)
            return false;

        await player.StopAsync();
        if (disconnect)
            await player.DisconnectAsync();

        return true;
    }
}
