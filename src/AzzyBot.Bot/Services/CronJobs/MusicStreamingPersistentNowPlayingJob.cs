using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Bot.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Lavalink4NET.Tracks;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class MusicStreamingPersistentNowPlayingJob(ILogger<MusicStreamingPersistentNowPlayingJob> logger, IMusicStreamingService musicStreaming, IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly ILogger<MusicStreamingPersistentNowPlayingJob> _logger = logger;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;
    private readonly IMusicStreamingService _musicStreaming = musicStreaming;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<MusicStreamingEntity> musicStreams = await _dbActions.ReadMusicStreamingAsync(loadGuild: true);
            if (musicStreams.Count is 0)
                return;

            await Task.WhenAll(musicStreams.Select(UpdateNowPlayingEmbedAsync));
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }

    private async Task UpdateNowPlayingEmbedAsync(MusicStreamingEntity stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.NowPlayingEmbedChannelId is <= 0)
            return;

        DiscordChannel? channel = await _botService.GetDiscordChannelAsync(stream.NowPlayingEmbedChannelId);
        if (channel is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordChannel), stream.Guild.UniqueId);
            return;
        }

        LavalinkTrack? track = _musicStreaming.NowPlaying(stream.Guild.UniqueId);
        TimeSpan? trackPosition = _musicStreaming.GetCurrentPosition(stream.Guild.UniqueId);
        if (track is null)
        {
            // Fail-fast: If there's no message to delete, we can skip directly to the end
            if (stream.NowPlayingEmbedMessageId is <= 0)
                return;

            DiscordMessage? delMsg = null;
            try
            {
                delMsg = await channel.GetMessageAsync(stream.NowPlayingEmbedMessageId);
            }
            catch (NotFoundException)
            {
                _logger.MessageNotFound(stream.NowPlayingEmbedMessageId, stream.NowPlayingEmbedChannelId, stream.Guild.UniqueId);
            }

            if (delMsg is not null)
                await delMsg.DeleteAsync();

            await _dbActions.UpdateMusicStreamingAsync(stream.Guild.UniqueId, nowPlayingEmbedMessageId: 0);

            return;
        }

        DiscordEmbed embed = EmbedBuilder.BuildMusicStreamingNowPlayingEmbed(track, trackPosition);
        if (stream.NowPlayingEmbedMessageId is > 0)
        {
            DiscordMessage? edMsg = null;
            try
            {
                edMsg = await channel.GetMessageAsync(stream.NowPlayingEmbedMessageId);
            }
            catch (NotFoundException)
            {
                _logger.MessageNotFound(stream.NowPlayingEmbedMessageId, stream.NowPlayingEmbedChannelId, stream.Guild.UniqueId);
            }

            if (edMsg is not null)
            {
                await edMsg.ModifyAsync(embed: embed);
                return;
            }
        }

        // This should only be reached if the message does not exist
        DiscordMessage? sMsg = await channel.SendMessageAsync(embed: embed);
        await _dbActions.UpdateMusicStreamingAsync(stream.Guild.UniqueId, nowPlayingEmbedMessageId: sMsg.Id);
    }
}
