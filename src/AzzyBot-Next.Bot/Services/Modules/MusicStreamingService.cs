using System;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services.Modules;

public sealed class MusicStreamingService(IAudioService audioService)
{
    private readonly IAudioService _audioService = audioService;

    public async Task<LavalinkPlayer?> GetLavalinkPlayerAsync(CommandContext context, bool connectToVoice = false)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        LavalinkPlayerOptions playerOptions = new() { SelfDeaf = true };
        PlayerRetrieveOptions retrieveOptions = new((connectToVoice) ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None, MemberVoiceStateBehavior.RequireSame);
        ulong channelId = context.Member?.VoiceState.Channel?.Id ?? 0;

        PlayerResult<LavalinkPlayer> player = await _audioService.Players.RetrieveAsync(context.Guild.Id, channelId, PlayerFactory.Default, Options.Create(playerOptions), retrieveOptions);
        if (player.IsSuccess)
            return player.Player;

        string errorMessage = player.Status switch
        {
            PlayerRetrieveStatus.BotNotConnected => "I'm not connected to a voice channel.",
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You need to be in a voice channel to use this command.",
            PlayerRetrieveStatus.VoiceChannelMismatch => "You need to be in the same voice channel as me to use this command.",
            _ => "An unknown error occurred while trying to retrieve the player."
        };

        await context.EditResponseAsync(errorMessage);

        return null;
    }

    public async Task<bool> JoinChannelAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, true);

        return player is not null;
    }

    public async Task<bool> LeaveChannelAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context);
        if (player is null)
            return false;

        await player.DisconnectAsync();

        return true;
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

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context);
        if (player is null)
            return false;

        await player.StopAsync();
        if (disconnect)
            await player.DisconnectAsync();

        return true;
    }
}
