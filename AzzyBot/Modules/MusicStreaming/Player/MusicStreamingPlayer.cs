using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Settings.MusicStreaming;
using AzzyBot.Strings.MusicStreaming;
using DSharpPlus.Entities;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using Lavalink4NET.Players;

namespace AzzyBot.Modules.MusicStreaming.Player;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is no static class")]
internal sealed class MusicStreamingPlayer : LavalinkPlayer, IInactivityPlayerListener
{
    internal MusicStreamingPlayer(IPlayerProperties<MusicStreamingPlayer, MusicStreamingPlayerOptions> properties) : base(properties)
    { }

    public async ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        // This method is called when the player was previously inactive and is now active again.
        cancellationToken.ThrowIfCancellationRequested();

        DiscordChannel? channel = MusicStreamingLavalink.GetRequestChannel;

        if (channel is null)
            return;

        await channel.SendMessageAsync(MusicStreamingStringBuilder.GetCustomPlayerIsActiveAgain);
    }

    public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        // This method is called when the player reached the inactivity deadline.
        cancellationToken.ThrowIfCancellationRequested();

        DiscordChannel? channel = MusicStreamingLavalink.GetRequestChannel;

        if (channel is null)
            return;

        await channel.SendMessageAsync(MusicStreamingStringBuilder.GetCustomPlayerLeaves);
    }

    public async ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        // This method is called when the player was previously active and is now inactive.
        cancellationToken.ThrowIfCancellationRequested();

        DiscordChannel? channel = MusicStreamingLavalink.GetRequestChannel;

        if (channel is null)
            return;

        await channel.SendMessageAsync(MusicStreamingStringBuilder.GetCustomPlayerIsInactive(MusicStreamingSettings.AutoDisconnectTime));
    }
}
