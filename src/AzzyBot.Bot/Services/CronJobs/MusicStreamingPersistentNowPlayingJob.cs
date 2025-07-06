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

using DSharpPlus.Entities;

using Lavalink4NET.Tracks;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class MusicStreamingPersistentNowPlayingJob(ILogger<MusicStreamingPersistentNowPlayingJob> logger, MusicStreamingService musicStreaming, DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly ILogger<MusicStreamingPersistentNowPlayingJob> _logger = logger;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;
    private readonly MusicStreamingService _musicStreaming = musicStreaming;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<MusicStreamingEntity> musicStreams = await _dbActions.ReadMusicStreamingAsync(loadGuild: true);
            if (!musicStreams.Any())
                return;

            foreach (MusicStreamingEntity stream in musicStreams)
            {
                await UpdateNowPlayingEmbedAsync(stream);
            }
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
            return;

        DiscordEmbed embed = EmbedBuilder.BuildMusicStreamingNowPlayingEmbed(track, trackPosition);
        if (stream.NowPlayingEmbedMessageId is > 0)
        {
            DiscordMessage? message = await channel.GetMessageAsync(stream.NowPlayingEmbedMessageId);
            if (message is not null)
            {
                await message.ModifyAsync(embed: embed);
                return;
            }
        }
        else
        {
            DiscordMessage? message = await channel.SendMessageAsync(embed: embed);
            await _dbActions.UpdateMusicStreamingAsync(stream.Guild.UniqueId, NowPlayingEmbedMessageId: message.Id);
        }
    }
}
