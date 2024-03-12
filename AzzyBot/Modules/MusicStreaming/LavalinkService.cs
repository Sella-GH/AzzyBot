using System;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Settings.AzuraCast;
using AzzyBot.Settings.MusicStreaming;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;

namespace AzzyBot.Modules.MusicStreaming;

internal static class LavalinkService
{
    private static async ValueTask<LavalinkPlayer?> GetPlayerAsync(InteractionContext ctx, bool connectToVoice = true)
    {
        PlayerRetrieveOptions retrieveOptions = new((connectToVoice) ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);
        LavalinkPlayerOptions playerOptions = new() { DisconnectOnDestroy = true, SelfDeaf = true };
        PlayerResult<LavalinkPlayer> result = await Program.GetAudioService.Players.RetrieveAsync(ctx.Guild.Id, ctx.Member?.VoiceState.Channel.Id, PlayerFactory.Default, Options.Create(playerOptions), retrieveOptions);

        if (!result.IsSuccess)
        {
            string errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected",
                PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel",
                _ => "An unknown error occurred"
            };

            DiscordFollowupMessageBuilder errorResponse = new DiscordFollowupMessageBuilder().WithContent(errorMessage).AsEphemeral();

            await ctx.FollowUpAsync(errorResponse);

            return null;
        }

        return result.Player;
    }

    internal static async Task PlayMusicAsync(InteractionContext ctx)
    {
        LavalinkPlayer? player = await GetPlayerAsync(ctx);

        if (player is null)
            return;

        if (player.CurrentTrack is not null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Player is busy"));
            return;
        }

        TrackLoadOptions trackLoadOptions = new()
        {
            SearchMode = TrackSearchMode.None,
            SearchBehavior = StrictSearchBehavior.Passthrough,
            CacheMode = CacheMode.Bypass
        };

        string url = string.Join("/", AzuraCastSettings.AzuraApiUrl.Replace("/api", string.Empty, StringComparison.OrdinalIgnoreCase), AzuraCastApiEnum.listen, MusicStreamingSettings.MountPointStub);
        LavalinkTrack? track = await Program.GetAudioService.Tracks.LoadTrackAsync(url, trackLoadOptions);

        if (track is null)
            return;

        await player.PlayAsync(track);
    }

    internal static async Task StopMusicAsync(InteractionContext ctx)
    {
        LavalinkPlayer? player = await GetPlayerAsync(ctx, false);

        if (player is null)
            return;

        if (player.CurrentTrack is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing playing"));
            return;
        }

        await player.StopAsync();
        await player.DisconnectAsync();
        await player.DisposeAsync();
    }
}
