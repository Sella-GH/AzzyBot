using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Lavalink4NET.Tracks;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class NowPlayingUpdateJob(ILogger<NowPlayingUpdateJob> logger, DbActions dbActions, DiscordClient discordClient, MusicStreamingService musicStreaming) : IJob
{
    private readonly ILogger<NowPlayingUpdateJob> _logger = logger;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordClient _discordClient = discordClient;
    private readonly MusicStreamingService _musicStreaming = musicStreaming;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        _logger.LogDebug("NowPlayingUpdateJob started");

        try
        {
            IReadOnlyList<GuildEntity> guilds = await _dbActions.GetGuildsAsync(loadEverything: true);
            var guildsWithNowPlaying = guilds.Where(g => g.Preferences.NowPlayingChannelId != 0).ToList();

            _logger.LogDebug("Found {Count} guilds with now-playing channels configured", guildsWithNowPlaying.Count);

            foreach (GuildEntity guildEntity in guildsWithNowPlaying)
            {
                try
                {
                    await UpdateNowPlayingForGuildAsync(guildEntity, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating now-playing for guild {GuildId}", guildEntity.UniqueId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NowPlayingUpdateJob");
        }

        _logger.LogDebug("NowPlayingUpdateJob completed");
    }

    private async Task UpdateNowPlayingForGuildAsync(GuildEntity guildEntity, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        DiscordGuild? guild = _discordClient.Guilds.GetValueOrDefault(guildEntity.UniqueId);
        if (guild is null)
        {
            _logger.LogWarning("Guild {GuildId} not found in Discord client", guildEntity.UniqueId);
            return;
        }

        DiscordChannel? channel = guild.GetChannel(guildEntity.Preferences.NowPlayingChannelId);
        if (channel is null)
        {
            _logger.LogWarning("Now-playing channel {ChannelId} not found in guild {GuildId}", 
                guildEntity.Preferences.NowPlayingChannelId, guildEntity.UniqueId);
            return;
        }

        try
        {
            // Get current playing track
            LavalinkTrack? track = await _musicStreaming.GetCurrentTrackAsync(guild.Id);
            TimeSpan? position = await _musicStreaming.GetCurrentTrackPositionAsync(guild.Id);

            DiscordEmbed? embed = null;
            string? content = null;

            if (track is not null && position is not null)
            {
                // Music is playing - create embed
                if ((track.Author is "AzzyBot.Bot" || track.Title is "AzzyBot.Bot" || track.Identifier is "AzzyBot.Bot") && position == TimeSpan.MinValue)
                {
                    content = "🎵 Playing AzuraCast stream";
                }
                else
                {
                    embed = EmbedBuilder.BuildMusicStreamingNowPlayingEmbed(track, position);
                }
            }
            else
            {
                // Nothing playing
                content = "🔇 Nothing is currently playing";
            }

            // Update or create the message
            if (guildEntity.Preferences.NowPlayingMessageId != 0)
            {
                // Try to edit existing message
                try
                {
                    DiscordMessage existingMessage = await channel.GetMessageAsync(guildEntity.Preferences.NowPlayingMessageId);
                    if (embed is not null)
                    {
                        await existingMessage.ModifyAsync(content: null, embed: embed);
                    }
                    else
                    {
                        await existingMessage.ModifyAsync(content: content, embed: null);
                    }
                    return;
                }
                catch (DiscordException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Message doesn't exist anymore, create a new one
                    _logger.LogDebug("Now-playing message {MessageId} not found, creating new one", guildEntity.Preferences.NowPlayingMessageId);
                }
            }

            // Create new message
            DiscordMessage newMessage;
            if (embed is not null)
            {
                newMessage = await channel.SendMessageAsync(embed: embed);
            }
            else
            {
                newMessage = await channel.SendMessageAsync(content: content);
            }

            // Update the message ID in database
            guildEntity.Preferences.NowPlayingMessageId = newMessage.Id;
            await _dbActions.UpdateGuildAsync(guildEntity);
        }
        catch (DiscordException ex)
        {
            _logger.LogError(ex, "Discord error updating now-playing for guild {GuildId}", guildEntity.UniqueId);
        }
    }
}