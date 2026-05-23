using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraPersistentNowPlayingJob(ILogger<AzuraPersistentNowPlayingJob> logger, IAzuraCastApiService apiService, IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly ILogger<AzuraPersistentNowPlayingJob> _logger = logger;
    private readonly IAzuraCastApiService _apiService = apiService;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

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

        string apiKey = (!string.IsNullOrEmpty(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(station.AzuraCast.AdminApiKey);
        Uri baseUrl = new(Crypto.Decrypt(station.AzuraCast.BaseUrl));

        AzuraNowPlayingDataRecord? nowPlaying = await _apiService.GetNowPlayingAsync(baseUrl, apiKey, station.StationId);
        if (nowPlaying?.IsOnline is not true)
        {
            // Fail-fast: If there's no message to delete, we can skip directly to the end
            if (station.Preferences.NowPlayingEmbedMessageId is <= 0)
                return;

            DiscordMessage? delMsg = null;
            try
            {
                delMsg = await channel.GetMessageAsync(station.Preferences.NowPlayingEmbedMessageId);
            }
            catch (NotFoundException)
            {
                _logger.MessageNotFound(station.Preferences.NowPlayingEmbedMessageId, station.Preferences.NowPlayingEmbedChannelId, station.AzuraCast.Guild.UniqueId);
            }

            if (delMsg is not null)
                await delMsg.DeleteAsync();

            await _dbActions.UpdateAzuraCastStationPreferencesAsync(station.AzuraCast.Guild.UniqueId, station.StationId, nowPlayingEmbedMessageId: 0);

            return;
        }

        DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicNowPlayingEmbed(nowPlaying);
        if (station.Preferences.NowPlayingEmbedMessageId is > 0)
        {
            DiscordMessage? edMsg = null;
            try
            {
                edMsg = await channel.GetMessageAsync(station.Preferences.NowPlayingEmbedMessageId);
            }
            catch (NotFoundException)
            {
                _logger.MessageNotFound(station.Preferences.NowPlayingEmbedMessageId, station.Preferences.NowPlayingEmbedChannelId, station.AzuraCast.Guild.UniqueId);
            }

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
