using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Modules.MusicStreaming.Settings;
using AzzyBot.Modules.MusicStreaming.Strings;
using DSharpPlus.Entities;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using Lavalink4NET.Players;

namespace AzzyBot.Modules.MusicStreaming.Player;

internal sealed class MsPlayer : LavalinkPlayer, IInactivityPlayerListener
{
    internal MsPlayer(IPlayerProperties<MsPlayer, MsPlayerOptions> properties) : base(properties)
    { }

    public async ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        // This method is called when the player was previously inactive and is now active again.
        cancellationToken.ThrowIfCancellationRequested();

        DiscordChannel? channel = MsLavalink.GetRequestChannel;

        if (channel is null)
            return;

        await channel.SendMessageAsync(MsStringBuilder.GetCustomPlayerIsActiveAgain);
    }

    public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        // This method is called when the player reached the inactivity deadline.
        cancellationToken.ThrowIfCancellationRequested();

        DiscordChannel? channel = MsLavalink.GetRequestChannel;

        if (channel is null)
            return;

        await channel.SendMessageAsync(MsStringBuilder.GetCustomPlayerLeaves);
    }

    public async ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        // This method is called when the player was previously active and is now inactive.
        cancellationToken.ThrowIfCancellationRequested();

        DiscordChannel? channel = MsLavalink.GetRequestChannel;
        DiscordChannel? voiceChannel = MsLavalink.GetVoiceChannel;

        if (channel is null || voiceChannel is null)
            return;

        if (voiceChannel.Users.Count == 1)
        {
            await channel.SendMessageAsync(MsStringBuilder.GetCustomPlayerIsInactiveUsers(MsSettings.AutoDisconnectTime));
            return;
        }

        await channel.SendMessageAsync(MsStringBuilder.GetCustomPlayerIsInactivePlaying(MsSettings.AutoDisconnectTime));
    }
}
