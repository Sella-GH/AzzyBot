using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.MusicStreaming.Player;
using AzzyBot.Modules.MusicStreaming.Settings;
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

internal static class MsLavalink
{
    internal static DiscordChannel? GetRequestChannel { get; private set; }
    internal static DiscordChannel? GetVoiceChannel { get; private set; }

    private static async ValueTask<MsPlayer?> GetPlayerAsync(InteractionContext ctx, bool allowConnect = false, bool requireChannel = true, bool firstTimeJoin = false, ImmutableArray<IPlayerPrecondition> preconditions = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PlayerRetrieveOptions retrieveOptions = new((allowConnect) ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None, (requireChannel) ? MemberVoiceStateBehavior.RequireSame : MemberVoiceStateBehavior.Ignore, preconditions);
        MsPlayerOptions playerOptions = new() { SelfDeaf = true };

        static ValueTask<MsPlayer> CreatePlayerAsync(IPlayerProperties<MsPlayer, MsPlayerOptions> properties, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties, nameof(properties));

            return ValueTask.FromResult(new MsPlayer(properties));
        }

        PlayerResult<MsPlayer> result = await AzzyBot.GetAudioService.Players.RetrieveAsync<MsPlayer, MsPlayerOptions>(ctx.Guild.Id, ctx.Member?.VoiceState.Channel.Id, CreatePlayerAsync, Options.Create(playerOptions), retrieveOptions, cancellationToken);

        if (result.IsSuccess)
        {
            GetRequestChannel = ctx.Channel;
            result.Player.FirstTimeJoining = firstTimeJoin;

            return result.Player;
        }

        DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.User.Id);
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(MsEmbedBuilder.BuildPreconditionErrorEmbed(CoreDiscordCommands.GetBestUsername(member.Username, member.Nickname), member.AvatarUrl, result)).AsEphemeral());

        return null;
    }

    internal static async Task<bool> DisconnectAsync(InteractionContext ctx)
    {
        MsPlayer? player = await GetPlayerAsync(ctx, false, true);

        if (player is null)
            return false;

        await player.DisconnectAsync();
        await player.DisposeAsync();

        return true;
    }

    internal static async Task<bool> JoinMusicAsync(InteractionContext ctx)
    {
        MsPlayer? player = await GetPlayerAsync(ctx, true, true, true, [PlayerPrecondition.NotPlaying]);

        if (player is null)
            return false;

        if (player.VoiceChannelId is 0)
            return false;

        GetVoiceChannel = ctx.Guild.GetChannel(player.VoiceChannelId);

        return true;
    }

    internal static async Task<bool> PlayMusicAsync(InteractionContext ctx)
    {
        MsPlayer? player = await GetPlayerAsync(ctx, true, true, true, [PlayerPrecondition.NotPlaying]);

        if (player is null)
            return false;

        if (player.VoiceChannelId is 0)
            return false;

        GetVoiceChannel = ctx.Guild.GetChannel(player.VoiceChannelId);

        TrackLoadOptions trackLoadOptions = new()
        {
            SearchMode = TrackSearchMode.None,
            SearchBehavior = StrictSearchBehavior.Passthrough,
            CacheMode = CacheMode.Bypass
        };

        bool hlsStream = MsSettings.MountPointStub.EndsWith("m3u8", StringComparison.OrdinalIgnoreCase);
        int streamingPort = MsSettings.StreamingPort;
        string azuraUrl = AcSettings.AzuraApiUrl.Replace("/api", string.Empty, StringComparison.OrdinalIgnoreCase);
        string url;

        if (hlsStream)
        {
            url = string.Join("/", azuraUrl, AcApiEnum.hls, MsSettings.MountPointStub);
        }
        else if (streamingPort is not 0)
        {
            url = string.Join("/", $"{azuraUrl}:{streamingPort}", MsSettings.MountPointStub);
        }
        else
        {
            url = string.Join("/", azuraUrl, AcApiEnum.listen, MsSettings.MountPointStub);
        }

        LavalinkTrack? track = await AzzyBot.GetAudioService.Tracks.LoadTrackAsync(url, trackLoadOptions);

        if (track is null)
            return false;

        await player.PlayAsync(track);

        return true;
    }

    internal static async Task<bool> SetVolumeAsync(InteractionContext ctx, float volume, bool reset)
    {
        MsPlayer? player = await GetPlayerAsync(ctx, false, true);

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

    internal static async Task<bool> StopMusicAsync(InteractionContext ctx, bool disconnect)
    {
        IPlayerPrecondition precondition = PlayerPrecondition.Any(PlayerPrecondition.Playing, PlayerPrecondition.Paused);
        MsPlayer? player = await GetPlayerAsync(ctx, false, true, false, [precondition]);

        if (player is null)
            return false;

        await player.StopAsync();

        if (disconnect)
            await DisconnectAsync(ctx);

        return true;
    }

    internal static async Task<DiscordEmbed> GetSongLyricsAsync(InteractionContext ctx)
    {
        NowPlayingData nowPlaying = await AcServer.GetNowPlayingAsync();

        Lyrics lyrics = await GetLyricsFromGeniusAsync(nowPlaying.Now_Playing.Song.Text);

        DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.User.Id);
        return MsEmbedBuilder.BuildLyricsEmbed(CoreDiscordCommands.GetBestUsername(member.Username, member.Nickname), member.AvatarUrl, lyrics, nowPlaying.Now_Playing.Song.Artist, nowPlaying.Now_Playing.Song.Title);
    }

    private static async Task<Lyrics> GetLyricsFromGeniusAsync(string search)
    {
        Lyrics? lyrics = await AzzyBot.GetAudioService.Tracks.GetGeniusLyricsAsync(search);

        return lyrics ?? new(string.Empty, string.Empty, new LyricsTrack(string.Empty, string.Empty, string.Empty, []), []);
    }
}
