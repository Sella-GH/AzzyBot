using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Core.Logging;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services.Modules;

public sealed class MusicStreamingService(IAudioService audioService, ILogger<MusicStreamingService> logger, DiscordBotService botService)
{
    private readonly IAudioService _audioService = audioService;
    private readonly ILogger<MusicStreamingService> _logger = logger;
    private readonly DiscordBotService _botService = botService;

    private async Task<LavalinkPlayer?> GetLavalinkPlayerAsync(SlashCommandContext context, bool useDefault = true, bool connectToVoice = false, bool suppressResponse = false, bool ignoreVoice = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);
        ArgumentNullException.ThrowIfNull(context.Member);

        if (context.Member.VoiceState is null)
        {
            _logger.UserNotConnected(context.User.GlobalName);

            if (!suppressResponse)
                await context.EditResponseAsync(GeneralStrings.VoiceNotConnected);

            if (!ignoreVoice)
                return null;
        }

        DiscordChannel? channel = context.Member.VoiceState?.Channel;
        ulong channelId = (channel is null) ? 0 : channel.Id;
        if (channel is null)
            _logger.UserNotConnectedSetChannelId();

        bool notConnecting = false;
        if (channel is null)
        {
            if (!suppressResponse)
                await context.EditResponseAsync(GeneralStrings.VoiceNoUser);

            return null;
        }
        else if (channelId is not 0)
        {
            DiscordMember? bot = await _botService.GetDiscordMemberAsync(context.Guild.Id);
            if (bot is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordMember), context.Guild.Id);
                notConnecting = true;
            }
            else if (!await _botService.CheckChannelPermissionsAsync(bot, channelId, DiscordPermissions.AccessChannels | DiscordPermissions.UseVoice))
            {
                notConnecting = true;
            }
        }

        if (notConnecting)
        {
            if (!suppressResponse)
                await context.EditResponseAsync("I don't have permission to connect to the voice channel.");

            return null;
        }

        PlayerResult<LavalinkPlayer> defaultPlayer;
        PlayerResult<QueuedLavalinkPlayer> queuedPlayer;
        PlayerRetrieveOptions retrieveOptions = new((connectToVoice) ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None, (ignoreVoice) ? MemberVoiceStateBehavior.Ignore : MemberVoiceStateBehavior.RequireSame);

        string errorMessage;
        if (useDefault)
        {
            LavalinkPlayerOptions defaultPlayerOptions = new()
            {
                DisconnectOnDestroy = true,
                SelfDeaf = true
            };

            defaultPlayer = await GetLavalinkDefaultPlayerAsync(context.Guild.Id, channelId, defaultPlayerOptions, retrieveOptions);
            if (defaultPlayer.IsSuccess)
                return defaultPlayer.Player;

            errorMessage = PostPlayerRetrieveError(defaultPlayer.Status, defaultPlayer.Precondition?.ToString());
        }
        else
        {
            QueuedLavalinkPlayerOptions queuedPlayerOptions = new()
            {
                ClearHistoryOnStop = false,
                ClearQueueOnStop = false,
                DefaultTrackRepeatMode = TrackRepeatMode.None,
                DisconnectOnDestroy = true,
                EnableAutoPlay = true,
                HistoryBehavior = TrackHistoryBehavior.Full,
                HistoryCapacity = int.MaxValue,
                ResetTrackRepeatOnStop = true,
                RespectTrackRepeatOnSkip = false,
                SelfDeaf = true
            };

            queuedPlayer = await GetLavalinkQueuedPlayerAsync(context.Guild.Id, channelId, queuedPlayerOptions, retrieveOptions);
            if (queuedPlayer.IsSuccess)
                return queuedPlayer.Player;

            errorMessage = PostPlayerRetrieveError(queuedPlayer.Status, queuedPlayer.Precondition?.ToString());
        }

        if (!suppressResponse)
            await context.EditResponseAsync(errorMessage);

        return null;
    }

    private async Task<PlayerResult<LavalinkPlayer>> GetLavalinkDefaultPlayerAsync(ulong guildId, ulong channelId, LavalinkPlayerOptions playerOptions, PlayerRetrieveOptions retrieveOptions)
    {
        ArgumentNullException.ThrowIfNull(playerOptions);

        return await _audioService.Players.RetrieveAsync(guildId, channelId, PlayerFactory.Default, Options.Create(playerOptions), retrieveOptions);
    }

    private async Task<PlayerResult<QueuedLavalinkPlayer>> GetLavalinkQueuedPlayerAsync(ulong guildId, ulong channelId, QueuedLavalinkPlayerOptions playerOptions, PlayerRetrieveOptions retrieveOptions)
    {
        ArgumentNullException.ThrowIfNull(playerOptions);

        return await _audioService.Players.RetrieveAsync(guildId, channelId, PlayerFactory.Queued, Options.Create(playerOptions), retrieveOptions);
    }

    [SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "These are not needed.")]
    private static string PostPlayerRetrieveError(PlayerRetrieveStatus status, string? precondition)
    {
        return status switch
        {
            PlayerRetrieveStatus.BotNotConnected => "I'm not connected to a voice channel.",
            PlayerRetrieveStatus.PreconditionFailed when precondition == PlayerPrecondition.NotPaused.ToString() => "I'm not paused.",
            PlayerRetrieveStatus.PreconditionFailed when precondition == PlayerPrecondition.NotPlaying.ToString() => "I'm not playing music.",
            PlayerRetrieveStatus.PreconditionFailed when precondition == PlayerPrecondition.Paused.ToString() => "I'm already paused.",
            PlayerRetrieveStatus.PreconditionFailed when precondition == PlayerPrecondition.Playing.ToString() => "I'm already playing music.",
            _ => "An unknown error occurred while trying to retrieve the player.",
        };
    }

    public async Task<bool> CheckIfPlayedMusicIsStationAsync(SlashCommandContext context, string station)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(station);

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, suppressResponse: true, ignoreVoice: true);

        // Guild has no player
        if (player is null)
            return false;

        // Player doesn't plays anything
        Uri? playedUri = player.CurrentTrack?.Uri;
        if (playedUri is null)
            return false;

        bool playingHls = playedUri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
        Uri stationUri = new((playingHls) ? station.Replace("/listen/", "/hls/", StringComparison.OrdinalIgnoreCase) : station);

        return (Uri.Compare(playedUri, stationUri, UriComponents.Host, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) is 0) && playedUri.AbsolutePath.StartsWith(stationUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> ClearQueueAsync(SlashCommandContext context, int position = -1)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await GetLavalinkPlayerAsync(context, useDefault: false) is not QueuedLavalinkPlayer player)
            return false;

        if (position is -1)
        {
            await player.Queue.ClearAsync();
        }
        else
        {
            await player.Queue.RemoveAtAsync(position);
        }

        return true;
    }

    public async Task<TimeSpan?> GetCurrentPositionAsync(SlashCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return (await GetLavalinkPlayerAsync(context, useDefault: false, suppressResponse: true, ignoreVoice: true) is QueuedLavalinkPlayer player)
               ? player.Position?.Position
               : null;
        }
        catch (InvalidOperationException)
        {
            return TimeSpan.MinValue;
        }
    }

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Code style")]
    public async Task<IEnumerable<ITrackQueueItem>?> HistoryAsync(SlashCommandContext context, bool queue = false)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await GetLavalinkPlayerAsync(context, useDefault: false, suppressResponse: true, ignoreVoice: true) is not QueuedLavalinkPlayer player)
            return [];

        return (queue) ? player.Queue : player.Queue.History;
    }

    public async Task<bool> JoinChannelAsync(SlashCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, connectToVoice: true);

        return player is not null;
    }

    public async Task<LavalinkTrack?> NowPlayingAsync(SlashCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return (await GetLavalinkPlayerAsync(context, useDefault: false, suppressResponse: true) is QueuedLavalinkPlayer player)
                ? player.CurrentTrack
                : null;
        }
        catch (InvalidOperationException)
        {
            return new() { Author = "AzzyBot.Bot", Identifier = "AzzyBot.Bot", Title = "AzzyBot.Bot" };
        }
    }

    public async Task<bool> PauseAsync(SlashCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await GetLavalinkPlayerAsync(context, useDefault: false) is not QueuedLavalinkPlayer player)
            return false;

        await player.PauseAsync();

        return true;
    }

    public async Task<string?> PlayMusicAsync(SlashCommandContext context, string query, TrackSearchMode searchMode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (await GetLavalinkPlayerAsync(context, useDefault: false, connectToVoice: true) is not QueuedLavalinkPlayer player)
            return null;

        TrackLoadOptions trackOptions = new()
        {
            CacheMode = CacheMode.Dynamic,
            SearchBehavior = StrictSearchBehavior.Passthrough,
            SearchMode = searchMode
        };

        TrackLoadResult tracks = await _audioService.Tracks.LoadTracksAsync(query, trackOptions);
        if (!tracks.IsSuccess)
        {
            await context.EditResponseAsync("An error occurred while trying to load the track.\nPlease check if you used the correct url for your selected provider!");
            return null;
        }

        foreach (LavalinkTrack track in tracks.Tracks)
        {
            await player.PlayAsync(track, true);
        }

        return (tracks.Tracks.Length > 2) ? $"I queued the playlist **{tracks.Playlist?.Name}** with **{tracks.Tracks.Length}** tracks." : $"I queued **{tracks.Track.Title}** by **{tracks.Track.Author}**";
    }

    public async Task<bool> PlayMountMusicAsync(SlashCommandContext context, string mountPoint)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(mountPoint);

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context, connectToVoice: true);
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

    public async Task<bool> ResumeAsync(SlashCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await GetLavalinkPlayerAsync(context, useDefault: false) is not QueuedLavalinkPlayer player)
            return false;

        await player.ResumeAsync();

        return true;
    }

    public async Task<bool> SetVolumeAsync(SlashCommandContext context, float volume, bool reset = false)
    {
        ArgumentNullException.ThrowIfNull(context);

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

    public async Task<bool> SkipSongAsync(SlashCommandContext context, int count = 1)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await GetLavalinkPlayerAsync(context, useDefault: false, suppressResponse: true) is not QueuedLavalinkPlayer player)
            return false;

        await player.SkipAsync(count);

        return true;
    }

    public async Task<bool> StopMusicAsync(SlashCommandContext context, bool disconnect = false)
    {
        ArgumentNullException.ThrowIfNull(context);

        LavalinkPlayer? player = await GetLavalinkPlayerAsync(context);
        if (player is null)
            return false;

        await player.StopAsync();
        if (disconnect)
            await player.DisconnectAsync();

        return true;
    }
}
