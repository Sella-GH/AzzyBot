using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.Core;
using AzzyBot.Settings.AzuraCast;
using AzzyBot.Settings.MusicStreaming;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lavalink4NET.Clients;
using Lavalink4NET.Integrations.LyricsJava;
using Lavalink4NET.Integrations.LyricsJava.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Rest.Entities;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingLavalink
{
    private static async ValueTask<LavalinkPlayer?> GetPlayerAsync(InteractionContext ctx, bool allowConnect = false, bool requireChannel = true, ImmutableArray<IPlayerPrecondition> preconditions = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PlayerRetrieveOptions retrieveOptions = new((allowConnect) ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None, (requireChannel) ? MemberVoiceStateBehavior.RequireSame : MemberVoiceStateBehavior.Ignore, preconditions);
        LavalinkPlayerOptions playerOptions = new() { SelfDeaf = true };
        PlayerResult<LavalinkPlayer> result = await Program.GetAudioService.Players.RetrieveAsync(ctx.Guild.Id, ctx.Member?.VoiceState.Channel.Id, PlayerFactory.Default, Options.Create(playerOptions), retrieveOptions, cancellationToken);

        if (result.IsSuccess)
            return result.Player;

        DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.User.Id);
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(MusicStreamingEmbedBuilder.BuildPreconditionErrorEmbed(CoreDiscordCommands.GetBestUsername(member.Username, member.Nickname), member.AvatarUrl, result)).AsEphemeral());

        return null;
    }

    internal static async Task<bool> DisconnectAsync(InteractionContext ctx)
    {
        LavalinkPlayer? player = await GetPlayerAsync(ctx, false, true) ?? throw new InvalidOperationException("Player is null");

        await player.DisconnectAsync();
        await player.DisposeAsync();

        return true;
    }

    internal static async Task<bool> JoinMusicAsync(InteractionContext ctx)
    {
        LavalinkPlayer? player = await GetPlayerAsync(ctx, true, true, [PlayerPrecondition.NotPlaying]) ?? throw new InvalidOperationException("Player is null");

        return player.VoiceChannelId is not 0;
    }

    internal static async Task<bool> PlayMusicAsync(InteractionContext ctx)
    {
        LavalinkPlayer? player = await GetPlayerAsync(ctx, true, true, [PlayerPrecondition.NotPlaying]) ?? throw new InvalidOperationException("Player is null");

        TrackLoadOptions trackLoadOptions = new()
        {
            SearchMode = TrackSearchMode.None,
            SearchBehavior = StrictSearchBehavior.Passthrough,
            CacheMode = CacheMode.Bypass
        };

        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl.Replace("/api", string.Empty, StringComparison.OrdinalIgnoreCase), AzuraCastApiEnum.listen, MusicStreamingSettings.MountPointStub);
        LavalinkTrack? track = await Program.GetAudioService.Tracks.LoadTrackAsync(url, trackLoadOptions);

        if (track is null)
            return false;

        await player.PlayAsync(track);

        return true;
    }

    internal static async Task<bool> SetVolumeAsync(InteractionContext ctx, float volume, bool reset)
    {
        LavalinkPlayer? player = await GetPlayerAsync(ctx, false, true) ?? throw new InvalidOperationException("Player is null");

        if (reset)
        {
            await player.SetVolumeAsync(100 / 100f);
            return true;
        }

        await player.SetVolumeAsync(volume / 100f);

        return true;
    }

    internal static async Task<bool> StopMusicAsync(InteractionContext ctx, bool disconnect)
    {
        IPlayerPrecondition precondition = PlayerPrecondition.Any(PlayerPrecondition.Playing, PlayerPrecondition.Paused);
        LavalinkPlayer? player = await GetPlayerAsync(ctx, false, true, [precondition]) ?? throw new InvalidOperationException("Player is null");

        await player.StopAsync();

        if (disconnect)
            await DisconnectAsync(ctx);

        return true;
    }

    internal static async Task<DiscordEmbed> GetSongLyricsAsync(InteractionContext ctx)
    {
        NowPlayingData nowPlaying = await AzuraCastServer.GetNowPlayingAsync();

        Lyrics lyrics = await GetLyricsFromGeniusAsync(nowPlaying.Now_Playing.Song.Text);

        DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.User.Id);
        return MusicStreamingEmbedBuilder.BuildLyricsEmbed(CoreDiscordCommands.GetBestUsername(member.Username, member.Nickname), member.AvatarUrl, lyrics, nowPlaying.Now_Playing.Song.Artist, nowPlaying.Now_Playing.Song.Title);
    }

    private static async Task<Lyrics> GetLyricsFromGeniusAsync(string search)
    {
        Lyrics? lyrics = await Program.GetAudioService.Tracks.GetGeniusLyricsAsync(search);

        return lyrics ?? new(string.Empty, string.Empty, new LyricsTrack(string.Empty, string.Empty, string.Empty, []), []);
    }
}
