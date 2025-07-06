using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraPersistentNowPlayingJob(ILogger<AzuraPersistentNowPlayingJob> logger, AzuraCastApiService apiService, DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly ILogger<AzuraPersistentNowPlayingJob> _logger = logger;
    private readonly AzuraCastApiService _apiService = apiService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<AzuraCastEntity> azuraCasts = await _dbActions.ReadAzuraCastsAsync(loadPrefs: true, loadStations: true, loadStationPrefs: true, loadGuild: true);
            if (!azuraCasts.Any())
                return;

            foreach (AzuraCastEntity azuraCast in azuraCasts.Where(static a => a.IsOnline))
            {
                foreach (AzuraCastStationEntity station in azuraCast.Stations.Where(static s => s.Preferences.NowPlayingEmbedChannelId is > 0))
                {
                    await UpdateNowPlayingEmbedAsync(station);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }

    private async Task UpdateNowPlayingEmbedAsync(AzuraCastStationEntity station)
    {
        ArgumentNullException.ThrowIfNull(station);

        if (station.Preferences.NowPlayingEmbedChannelId is <= 0)
            return;

        DiscordChannel? channel = await _botService.GetDiscordChannelAsync(station.Preferences.NowPlayingEmbedChannelId);
        if (channel is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordChannel), station.AzuraCast.Guild.UniqueId);
            return;
        }

        Uri baseUri = new(Crypto.Decrypt(station.AzuraCast.BaseUrl));
        AzuraNowPlayingDataRecord? nowPlaying = await _apiService.GetNowPlayingAsync(baseUri, station.StationId);
        if (nowPlaying?.IsOnline is not true)
        {
            DiscordMessage? delMsg = await channel.GetMessageAsync(station.Preferences.NowPlayingEmbedMessageId);
            if (delMsg is not null)
            {
                await delMsg.DeleteAsync();
                await _dbActions.UpdateAzuraCastStationPreferencesAsync(station.AzuraCast.Guild.UniqueId, station.StationId, nowPlayingEmbedMessageId: 0);
            }

            return;
        }

        DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicNowPlayingEmbed(nowPlaying);
        if (station.Preferences.NowPlayingEmbedMessageId is > 0)
        {
            DiscordMessage? edMsg = await channel.GetMessageAsync(station.Preferences.NowPlayingEmbedMessageId);
            if (edMsg is not null)
            {
                await edMsg.ModifyAsync(embed: embed);
                return;
            }
        }

        // This should only be reached if the message does not exist
        DiscordMessage sMsg = await channel.SendMessageAsync(embed: embed);
        await _dbActions.UpdateAzuraCastStationPreferencesAsync(station.AzuraCast.Guild.UniqueId, station.StationId, nowPlayingEmbedMessageId: sMsg.Id);
    }
}
